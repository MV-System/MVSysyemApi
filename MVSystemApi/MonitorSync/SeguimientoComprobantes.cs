using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace MVSystemApi.MonitorSync
{
    /// <summary>Un comprobante (factura o nota de crédito) que se está observando hasta que el proveedor dé una respuesta final.</summary>
    public class ItemSeguimiento
    {
        public string Clave => $"{Rnc}|{Encf}";
        /// <summary>1 = EasyConexión, 2 = MicroStar.</summary>
        public int Proveedor { get; set; }
        public string Empresa { get; set; }
        public string Rnc { get; set; }
        public int NumeroFactura { get; set; }
        public string Literal { get; set; }
        public string Encf { get; set; }
        public int TipoNcf { get; set; }
        public bool EsNotaCredito { get; set; }
        /// <summary>Identificador del documento en EasyConexión (proveedor 1).</summary>
        public int? DocumentId { get; set; }
        public string Usuario { get; set; }
        public DateTime InicioUtc { get; set; } = DateTime.UtcNow;
        public DateTime UltimaConsultaUtc { get; set; } = DateTime.MinValue;
        /// <summary>Ya se avisó de un problema de este comprobante (para no repetir el aviso y, si se resuelve, avisar que se resolvió).</summary>
        public bool Alertado { get; set; }
    }

    /// <summary>
    /// Seguimiento en memoria de los comprobantes que quedaron pendientes al emitirse, y avisos (correo / WhatsApp, con la
    /// configuración de Monitor) de los que dan problemas. Solo avisa cuando hay un problema: si todo sale bien no envía nada.
    /// Registrar no hace nada si Monitor:Habilitado = false. Nunca lanza: no puede afectar la factura ni la nota de crédito.
    /// </summary>
    public class SeguimientoComprobantes
    {
        private const int MaximoEnSeguimiento = 500;

        private readonly ConcurrentDictionary<string, ItemSeguimiento> _items = new();
        private readonly MonitorOptions _opciones;
        private readonly NotificadorAlertas _notificador;
        private readonly ILogger<SeguimientoComprobantes> _logger;

        public SeguimientoComprobantes(IOptions<MonitorOptions> opciones, NotificadorAlertas notificador, ILogger<SeguimientoComprobantes> logger)
        {
            _opciones = opciones.Value;
            _notificador = notificador;
            _logger = logger;
        }

        public bool Habilitado => _opciones.Habilitado;

        public void Registrar(ItemSeguimiento item)
        {
            try
            {
                if (!Habilitado || item == null || string.IsNullOrWhiteSpace(item.Encf)) return;
                if (_items.Count >= MaximoEnSeguimiento) return;
                _items[item.Clave] = item;
                _logger.LogInformation("[Comprobantes] En seguimiento: {Encf} (factura {Factura}, proveedor {Proveedor})", item.Encf, item.NumeroFactura, item.Proveedor);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Comprobantes] No se pudo registrar el seguimiento");
            }
        }

        public IReadOnlyList<ItemSeguimiento> Pendientes() => _items.Values.ToList();

        public void Quitar(string clave) => _items.TryRemove(clave, out _);

        // ---- Avisos ----
        public Task AlertarProblemaAsync(ItemSeguimiento i, string estado, string detalle)
        {
            var m = MensajesComprobantes.Problema(i, estado, detalle);
            return EnviarAsync(m, i);
        }

        public Task AlertarObservacionesAsync(ItemSeguimiento i, string detalle)
        {
            var m = MensajesComprobantes.Observaciones(i, detalle);
            return EnviarAsync(m, i);
        }

        public Task AlertarSinRespuestaAsync(ItemSeguimiento i, int minutos)
        {
            var m = MensajesComprobantes.SinRespuesta(i, minutos);
            return EnviarAsync(m, i);
        }

        public Task AlertarResueltoAsync(ItemSeguimiento i)
        {
            var m = MensajesComprobantes.Resuelto(i);
            return EnviarAsync(m, i);
        }

        private async Task EnviarAsync((string Asunto, string Cuerpo, string Corto) m, ItemSeguimiento i)
        {
            if (!Habilitado) return;
            try
            {
                _logger.LogWarning("[Comprobantes] {Asunto}", m.Asunto);
                var envio = await _notificador.EnviarAsync(m.Asunto, m.Cuerpo, m.Corto);
                _logger.LogInformation("[Comprobantes] Aviso enviado para {Encf} ({Envio})", i?.Encf, envio);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Comprobantes] No se pudo enviar el aviso de {Encf}", i?.Encf);
            }
        }
    }
}
