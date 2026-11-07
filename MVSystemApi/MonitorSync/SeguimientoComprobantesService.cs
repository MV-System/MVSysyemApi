using Microsoft.Extensions.Options;
using MVSystemApi.Services;

namespace MVSystemApi.MonitorSync
{
    /// <summary>
    /// Servicio de fondo de comprobantes. Hace dos cosas, solo lectura y sin afectar facturas:
    /// 1) observa los comprobantes del proveedor 1 que quedaron pendientes al emitirse (cada 10 s los primeros 2 min, luego cada minuto);
    /// 2) barre la tabla remota del proveedor 2 en busca de comprobantes rechazados, con error o sin respuesta.
    /// Solo avisa cuando hay un problema (y cuando uno ya avisado se resuelve). No usa AccesoDatos: no hay sesion de usuario.
    /// </summary>
    public class SeguimientoComprobantesService : BackgroundService
    {
        private sealed class Visto
        {
            public ItemSeguimiento Item;
            public int Veces;
            public bool Alertado;
            public string UltimoEstado;
        }

        private readonly SeguimientoComprobantes _seguimiento;
        private readonly Proveedor2Lector _lector;
        private readonly IHttpClientFactory _http;
        private readonly IConfiguration _config;
        private readonly MonitorOptions _opciones;
        private readonly ILogger<SeguimientoComprobantesService> _logger;
        private readonly Dictionary<string, Visto> _vistos = new();
        private DateTime _ultimoBarridoUtc = DateTime.MinValue;

        public SeguimientoComprobantesService(SeguimientoComprobantes seguimiento, Proveedor2Lector lector, IHttpClientFactory http,
            IConfiguration config, IOptions<MonitorOptions> opciones, ILogger<SeguimientoComprobantesService> logger)
        {
            _seguimiento = seguimiento;
            _lector = lector;
            _http = http;
            _config = config;
            _opciones = opciones.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_opciones.Habilitado) return;
            _logger.LogInformation("[Comprobantes] Seguimiento de comprobantes activo");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ObservarProveedor1Async(stoppingToken);

                    if (DateTime.UtcNow - _ultimoBarridoUtc >= TimeSpan.FromMinutes(Math.Max(1, _opciones.IntervaloComprobantesMinutos)))
                    {
                        _ultimoBarridoUtc = DateTime.UtcNow;
                        await BarrerProveedor2Async(stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[Comprobantes] Error en el ciclo de seguimiento");
                }

                try { await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); }
                catch (OperationCanceledException) { break; }
            }
        }

        // ---- Proveedor 1 ----
        private async Task ObservarProveedor1Async(CancellationToken ct)
        {
            foreach (var item in _seguimiento.Pendientes())
            {
                var transcurrido = DateTime.UtcNow - item.InicioUtc;
                var espera = transcurrido < TimeSpan.FromMinutes(2) ? TimeSpan.FromSeconds(10) : TimeSpan.FromMinutes(1);
                if (DateTime.UtcNow - item.UltimaConsultaUtc < espera) continue;
                item.UltimaConsultaUtc = DateTime.UtcNow;

                if (item.DocumentId == null)
                {
                    _seguimiento.Quitar(item.Clave);
                    continue;
                }

                var (estatus, motivo) = await ConsultarProveedor1Async(item.DocumentId.Value, ct);

                if (estatus == "APRO")
                {
                    _seguimiento.Quitar(item.Clave);
                    // Aprobada pero con observaciones de la DGII (el comentario limpio es "[0]")
                    if (!string.IsNullOrWhiteSpace(motivo) && !motivo.TrimStart().StartsWith("[0]"))
                        await _seguimiento.AlertarObservacionesAsync(item, motivo);
                }
                else if (!string.IsNullOrEmpty(estatus) && estatus != "PEND")
                {
                    _seguimiento.Quitar(item.Clave);
                    await _seguimiento.AlertarProblemaAsync(item, estatus, motivo);
                }
                else if (transcurrido > TimeSpan.FromMinutes(Math.Max(1, _opciones.MinutosSeguimiento)))
                {
                    _seguimiento.Quitar(item.Clave);
                    await _seguimiento.AlertarSinRespuestaAsync(item, _opciones.MinutosSeguimiento);
                }
            }
        }

        private async Task<(string Estatus, string Motivo)> ConsultarProveedor1Async(int documentId, CancellationToken ct)
        {
            try
            {
                var http = _http.CreateClient("FacEDGI");
                var respuesta = await http.GetAsync($"{_config["FacEDGI:BaseUrl"]}/invoices/{documentId}", ct);
                if (!respuesta.IsSuccessStatusCode) return (null, $"HTTP {(int)respuesta.StatusCode}");
                var json = await respuesta.Content.ReadAsStringAsync(ct);
                var estatus = FacEDGIService.LeerTextoJson(json, "estatus", "status")?.Trim().ToUpperInvariant() ?? "";
                var motivo = FacEDGIService.LeerTextoJson(json, "dgiiMessage") ?? FacEDGIService.LeerMotivoSeguimiento(json);
                return (estatus, motivo);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "[Comprobantes] No se pudo consultar el documento {Id} en el proveedor 1", documentId);
                return (null, ex.Message);
            }
        }

        // ---- Proveedor 2 ----
        private async Task BarrerProveedor2Async(CancellationToken ct)
        {
            if (!_lector.Disponible) return;

            var empresas = await _lector.EmpresasAsync(ct);
            if (empresas.Count == 0) return;
            var filas = await _lector.NoAceptadosAsync(empresas.Keys.ToList(), _opciones.DiasRevision, ct);

            var claves = new HashSet<string>();
            foreach (var f in filas)
            {
                if (string.IsNullOrWhiteSpace(f.Encf)) continue;
                var clave = $"{f.Rnc}|{f.Encf}";
                claves.Add(clave);

                if (!_vistos.TryGetValue(clave, out var visto))
                {
                    int.TryParse(f.NumDocumento, out var numero);
                    visto = new Visto
                    {
                        Item = new ItemSeguimiento
                        {
                            Proveedor = 2,
                            Empresa = empresas.TryGetValue(f.Rnc, out var nombre) ? nombre : f.Rnc,
                            Rnc = f.Rnc,
                            NumeroFactura = numero,
                            Encf = f.Encf,
                            TipoNcf = f.TipoNcf,
                            EsNotaCredito = string.Equals(f.TipoDocumento, "NC", StringComparison.OrdinalIgnoreCase),
                            InicioUtc = f.FechaRegistro == DateTime.MinValue ? DateTime.UtcNow : f.FechaRegistro.AddHours(4),
                        }
                    };
                    _vistos[clave] = visto;
                }

                visto.Veces++;
                visto.UltimoEstado = f.Estatus;
                if (visto.Alertado) continue;

                var final = f.Estatus == "RECHAZADO_DGII" || f.Estatus == "ERROR";
                var minutos = (DateTime.UtcNow - visto.Item.InicioUtc).TotalMinutes;
                var sinRespuesta = !final && minutos >= Math.Max(1, _opciones.MinutosSinRespuesta);

                // Un estado de problema debe repetirse en revisiones consecutivas (no avisar por algo que el proveedor corrige solo)
                if (final && visto.Veces >= Math.Max(1, _opciones.ConfirmacionesMinimas))
                {
                    visto.Alertado = true;
                    await _seguimiento.AlertarProblemaAsync(visto.Item, f.Estatus, f.Error);
                }
                else if (sinRespuesta)
                {
                    visto.Alertado = true;
                    await _seguimiento.AlertarSinRespuestaAsync(visto.Item, (int)minutos);
                }
            }

            // Los que ya no figuran como no aceptados: si se avisó del problema, avisar que se resolvió
            foreach (var clave in _vistos.Keys.Where(k => !claves.Contains(k)).ToList())
            {
                var visto = _vistos[clave];
                _vistos.Remove(clave);
                if (visto.Alertado)
                    await _seguimiento.AlertarResueltoAsync(visto.Item);
            }
        }
    }
}
