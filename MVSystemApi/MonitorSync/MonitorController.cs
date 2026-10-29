using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MVSystemApi.MonitorSync
{
    [ApiController]
    [Route("Monitor")]
    public class MonitorController : ControllerBase
    {
        private readonly MonitorOptions _opciones;
        private readonly RevisorSincronizacion _revisor;
        private readonly NotificadorAlertas _notificador;

        public MonitorController(IOptions<MonitorOptions> opciones, RevisorSincronizacion revisor, NotificadorAlertas notificador)
        {
            _opciones = opciones.Value;
            _revisor = revisor;
            _notificador = notificador;
        }

        /// <summary>
        /// Ejecuta una revision ahora mismo (solo lectura) y devuelve lo que encontro. Con enviar=true manda ademas
        /// un aviso de prueba por los canales configurados. No afecta el seguimiento de alertas del monitor.
        /// GET: Monitor/Probar?completa=false&amp;enviar=false
        /// Solo para los usuarios de Monitor:UsuariosAutorizados.
        /// </summary>
        [HttpGet("Probar")]
        public async Task<IActionResult> Probar([FromQuery] bool completa = false, [FromQuery] bool enviar = false, CancellationToken ct = default)
        {
            var usuario = User.Identity?.Name;
            if (!_opciones.UsuariosAutorizados.Any(u => string.Equals(u, usuario, StringComparison.OrdinalIgnoreCase)))
                return StatusCode(403, "No tiene permisos para realizar esta accion.");

            var revision = await _revisor.RevisarAsync(completa ? AlcanceRevision.Completa : AlcanceRevision.Liviana, ct);

            string envio = null;
            if (enviar)
            {
                var m = MensajesMonitor.ResumenDiario(revision, revision.Hallazgos, _opciones.MaxItemsPorAviso);
                envio = await _notificador.EnviarAsync("[MVSystem] PRUEBA del monitor de sincronizacion - " + m.Asunto, m.Cuerpo, "PRUEBA - " + m.Corto);
            }

            return Ok(new
            {
                alcance = revision.Alcance.ToString(),
                habilitado = _opciones.Habilitado,
                resumen = revision.Resumen,
                errores = revision.Errores,
                totalDiferencias = revision.Hallazgos.Count,
                diferencias = revision.Hallazgos.Take(100).Select(h => new { h.Empresa, h.Categoria, h.Detalle }),
                envio,
            });
        }
    }
}
