using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MVSystemApi.Interfaz;
using MVSystemApi.Model;
using MVSystemApi.Model_Negocio;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MVSystemApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TrackingController : ControllerBase
    {
        private readonly ITracking _trackingService;
        private readonly WhatsAppService _whatsAppService;
        private readonly ILogger<TrackingController> _logger;

        public TrackingController(
            ITracking trackingService,
            WhatsAppService whatsAppService,
            ILogger<TrackingController> logger)
        {
            _trackingService = trackingService;
            _whatsAppService = whatsAppService;
            _logger = logger;
        }

        /// <summary>
        /// Registra una interacción del usuario
        /// POST /api/tracking/log
        /// </summary>
        [HttpPost("log")]
        public async Task<IActionResult> RegistrarLog([FromBody] TrackingLogDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new TrackingResponseDto
                    {
                        Success = false,
                        Message = "Datos inválidos",
                        Data = ModelState
                    });
                }

                var log = new TrackingLog
                {
                    Usuario = dto.Usuario,
                    Modulo = dto.Modulo,
                    Accion = dto.Accion,
                    Metadata = dto.Metadata,
                    DireccionIP = ObtenerDireccionIP(),
                    UserAgent = Request.Headers["User-Agent"].ToString()
                    // FechaHora se asignará en SQL con DATEADD(HOUR, -4, GETDATE())
                };

                var idTracking = await _trackingService.RegistrarTracking(log);
                log.IdTracking = idTracking;

                // Verificar si debe enviar notificación
                if (await _trackingService.DebeNotificar(dto.Modulo, dto.Accion))
                {
                    _ = Task.Run(async () =>
                    {
                        var enviado = await _whatsAppService.NotificarInteraccion(log);
                        if (enviado)
                        {
                            await _trackingService.MarcarComoNotificado(idTracking);
                        }
                    });
                }

                return Ok(new TrackingResponseDto
                {
                    Success = true,
                    Message = "Interacción registrada correctamente",
                    Data = new { IdTracking = idTracking }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al registrar tracking: {ex.Message}");
                return StatusCode(500, new TrackingResponseDto
                {
                    Success = false,
                    Message = "Error interno del servidor",
                    Data = ex.Message
                });
            }
        }

        /// <summary>
        /// Obtiene el historial de tracking
        /// GET /api/tracking/history
        /// </summary>
        [HttpGet("history")]
        public async Task<IActionResult> ObtenerHistorial(
            [FromQuery] string usuario = null,
            [FromQuery] string modulo = null,
            [FromQuery] DateTime? fechaDesde = null,
            [FromQuery] DateTime? fechaHasta = null,
            [FromQuery] int top = 100)
        {
            try
            {
                var historial = await _trackingService.ObtenerHistorial(
                    usuario, modulo, fechaDesde, fechaHasta, top);

                return Ok(new TrackingResponseDto
                {
                    Success = true,
                    Message = $"Se encontraron {historial.Count} registros",
                    Data = historial
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al obtener historial: {ex.Message}");
                return StatusCode(500, new TrackingResponseDto
                {
                    Success = false,
                    Message = "Error al obtener historial",
                    Data = ex.Message
                });
            }
        }

        /// <summary>
        /// Obtiene estadísticas de uso por módulo
        /// GET /api/tracking/stats/modules
        /// </summary>
        [HttpGet("stats/modules")]
        public async Task<IActionResult> ObtenerEstadisticasModulos(
            [FromQuery] DateTime? fechaDesde = null,
            [FromQuery] DateTime? fechaHasta = null)
        {
            try
            {
                var stats = await _trackingService.ObtenerEstadisticasPorModulo(fechaDesde, fechaHasta);

                return Ok(new TrackingResponseDto
                {
                    Success = true,
                    Message = "Estadísticas obtenidas correctamente",
                    Data = stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al obtener estadísticas: {ex.Message}");
                return StatusCode(500, new TrackingResponseDto
                {
                    Success = false,
                    Message = "Error al obtener estadísticas",
                    Data = ex.Message
                });
            }
        }

        /// <summary>
        /// Obtiene los usuarios más activos
        /// GET /api/tracking/stats/users
        /// </summary>
        [HttpGet("stats/users")]
        public async Task<IActionResult> ObtenerUsuariosActivos([FromQuery] int top = 10)
        {
            try
            {
                var usuarios = await _trackingService.ObtenerUsuariosMasActivos(top);

                return Ok(new TrackingResponseDto
                {
                    Success = true,
                    Message = "Top usuarios obtenido correctamente",
                    Data = usuarios.Select(u => new { Usuario = u.Item1, Interacciones = u.Item2 })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al obtener usuarios activos: {ex.Message}");
                return StatusCode(500, new TrackingResponseDto
                {
                    Success = false,
                    Message = "Error al obtener usuarios activos",
                    Data = ex.Message
                });
            }
        }

        /// <summary>
        /// Envía una notificación manual de WhatsApp
        /// POST /api/tracking/notify
        /// </summary>
        [HttpPost("notify")]
        public async Task<IActionResult> EnviarNotificacion([FromBody] WhatsAppMessageDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new TrackingResponseDto
                    {
                        Success = false,
                        Message = "Datos inválidos"
                    });
                }

                var enviado = await _whatsAppService.EnviarMensaje(dto.PhoneNumber, dto.Message);

                return Ok(new TrackingResponseDto
                {
                    Success = enviado,
                    Message = enviado ? "Mensaje enviado correctamente" : "Error al enviar mensaje"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al enviar notificación: {ex.Message}");
                return StatusCode(500, new TrackingResponseDto
                {
                    Success = false,
                    Message = "Error al enviar notificación",
                    Data = ex.Message
                });
            }
        }

        /// <summary>
        /// Verifica el estado del servicio WhatsApp
        /// GET /api/tracking/whatsapp/status
        /// </summary>
        [HttpGet("whatsapp/status")]
        public async Task<IActionResult> VerificarEstadoWhatsApp()
        {
            try
            {
                var estado = await _whatsAppService.VerificarEstado();

                return Ok(new TrackingResponseDto
                {
                    Success = estado,
                    Message = estado ? "Servicio WhatsApp activo" : "Servicio WhatsApp no disponible",
                    Data = new { Activo = estado }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new TrackingResponseDto
                {
                    Success = false,
                    Message = "Error al verificar estado",
                    Data = ex.Message
                });
            }
        }

        /// <summary>
        /// Obtiene la dirección IP del cliente
        /// </summary>
        private string ObtenerDireccionIP()
        {
            var ip = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (string.IsNullOrEmpty(ip))
            {
                ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            }
            return ip;
        }

        /// <summary>
        /// Determina si debe enviar notificación según módulo y acción
        /// </summary>
        // M�todo DebeNotificar movido a Tracking_Negocio para usar configuraci�n de BD
    }
}

