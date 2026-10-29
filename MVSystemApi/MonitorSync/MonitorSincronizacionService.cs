using Microsoft.Extensions.Options;

namespace MVSystemApi.MonitorSync
{
    /// <summary>
    /// Revisa periodicamente que la base principal y su espejo esten iguales y avisa si no.
    /// Apagado por defecto (Monitor:Habilitado). Revision liviana cada N minutos y completa una vez al dia.
    /// </summary>
    public class MonitorSincronizacionService : BackgroundService
    {
        private readonly MonitorOptions _opciones;
        private readonly RevisorSincronizacion _revisor;
        private readonly EstadoAlertas _estado;
        private readonly NotificadorAlertas _notificador;
        private readonly ILogger<MonitorSincronizacionService> _logger;

        private DateTime _ultimaCompleta = DateTime.MinValue;
        private DateTime _ultimoResumen = DateTime.MinValue;
        private ResultadoRevision _ultimaRevision;

        public MonitorSincronizacionService(
            IOptions<MonitorOptions> opciones,
            RevisorSincronizacion revisor,
            EstadoAlertas estado,
            NotificadorAlertas notificador,
            ILogger<MonitorSincronizacionService> logger)
        {
            _opciones = opciones.Value;
            _revisor = revisor;
            _estado = estado;
            _notificador = notificador;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_opciones.Habilitado)
            {
                _logger.LogInformation("[Monitor] Deshabilitado (Monitor:Habilitado = false)");
                return;
            }

            var intervalo = TimeSpan.FromMinutes(Math.Max(1, _opciones.IntervaloMinutos));
            _logger.LogInformation("[Monitor] Iniciado: revision cada {Minutos} min, completa a las {Completa}, resumen a las {Resumen}",
                intervalo.TotalMinutes, _opciones.HoraRevisionCompleta, _opciones.HoraResumenDiario);

            try
            {
                // Se deja arrancar el API antes de la primera revision
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

                using var temporizador = new PeriodicTimer(intervalo);
                do
                {
                    try
                    {
                        await EjecutarCicloAsync(stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        // Un fallo del ciclo no debe detener el monitor
                        _logger.LogError(ex, "[Monitor] Error en el ciclo de revision");
                    }
                }
                while (await temporizador.WaitForNextTickAsync(stoppingToken));
            }
            catch (OperationCanceledException)
            {
                // El API se esta deteniendo
            }
        }

        private async Task EjecutarCicloAsync(CancellationToken ct)
        {
            var ahoraRD = DateTime.UtcNow.AddHours(-4); // Republica Dominicana (UTC-4, sin horario de verano)
            var hoy = ahoraRD.Date;

            bool completa = _ultimaCompleta != hoy && ahoraRD.TimeOfDay >= Hora(_opciones.HoraRevisionCompleta);
            var alcance = completa ? AlcanceRevision.Completa : AlcanceRevision.Liviana;

            var revision = await _revisor.RevisarAsync(alcance, ct);
            if (completa) _ultimaCompleta = hoy;
            _ultimaRevision = revision;

            _logger.LogInformation("[Monitor] Revision {Alcance}: {Hallazgos} diferencia(s), {Errores} error(es). {Resumen}",
                alcance, revision.Hallazgos.Count, revision.Errores.Count, string.Join(" | ", revision.Resumen));

            var (nuevos, resueltos) = _estado.Procesar(revision, _opciones.ConfirmacionesMinimas);

            if (nuevos.Count > 0)
            {
                var m = MensajesMonitor.Nuevos(nuevos, revision, _opciones.MaxItemsPorAviso);
                var envio = await _notificador.EnviarAsync(m.Asunto, m.Cuerpo, m.Corto);
                _logger.LogWarning("[Monitor] {Cantidad} diferencia(s) nueva(s) avisada(s) ({Envio})", nuevos.Count, envio);
            }

            if (resueltos.Count > 0)
            {
                var m = MensajesMonitor.Resueltos(resueltos);
                var envio = await _notificador.EnviarAsync(m.Asunto, m.Cuerpo, m.Corto);
                _logger.LogInformation("[Monitor] {Cantidad} diferencia(s) resuelta(s) avisada(s) ({Envio})", resueltos.Count, envio);
            }

            // Resumen diario: ventana de 3 horas para que un reinicio a media tarde no genere otro aviso
            var horaResumen = Hora(_opciones.HoraResumenDiario);
            if (_ultimoResumen != hoy && ahoraRD.TimeOfDay >= horaResumen && ahoraRD.TimeOfDay < horaResumen + TimeSpan.FromHours(3))
            {
                var m = MensajesMonitor.ResumenDiario(_ultimaRevision, _estado.Pendientes(), _opciones.MaxItemsPorAviso);
                var envio = await _notificador.EnviarAsync(m.Asunto, m.Cuerpo, m.Corto);
                _ultimoResumen = hoy;
                _logger.LogInformation("[Monitor] Resumen diario enviado ({Envio})", envio);
            }
        }

        private static TimeSpan Hora(string texto) =>
            TimeSpan.TryParse(texto, out var hora) ? hora : TimeSpan.FromHours(2);
    }
}
