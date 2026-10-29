using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MVSystemApi.Model;
using MVSystemApi.Services;

namespace MVSystemApi.Controllers
{
    /// <summary>
    /// Registro del uso del sistema (pantallas, botones y sesiones). Registrar y Salir los usa cualquier usuario;
    /// las consultas son solo para quien tenga el permiso ModoDesarrollador.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsoSistemaController : ControllerBase
    {
        private readonly UsoSistemaService _servicio;
        private readonly ILogger<UsoSistemaController> _logger;

        public UsoSistemaController(UsoSistemaService servicio, ILogger<UsoSistemaController> logger)
        {
            _servicio = servicio;
            _logger = logger;
        }

        /// <summary>POST api/UsoSistema/Registrar: latido de la sesion mas los eventos pendientes.</summary>
        [HttpPost("Registrar")]
        public async Task<IActionResult> Registrar([FromBody] UsoRegistroDto dto)
        {
            if (dto == null || dto.SesionId == Guid.Empty) return BadRequest();
            try
            {
                var ip = Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? HttpContext.Connection.RemoteIpAddress?.ToString();
                await _servicio.RegistrarAsync(dto, User.Identity?.Name, EmpresaDelUsuario(), ip, Request.Headers["User-Agent"].ToString());
                return Ok();
            }
            catch (Exception ex)
            {
                // El registro de uso nunca debe afectar el trabajo del usuario
                _logger.LogError(ex, "No se pudo registrar el uso del sistema");
                return StatusCode(500);
            }
        }

        /// <summary>POST api/UsoSistema/Salir: marca la sesion como terminada.</summary>
        [HttpPost("Salir")]
        public async Task<IActionResult> Salir([FromBody] UsoSalidaDto dto)
        {
            if (dto == null || dto.SesionId == Guid.Empty) return BadRequest();
            try
            {
                await _servicio.SalirAsync(dto.SesionId, User.Identity?.Name);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo cerrar la sesion de uso");
                return StatusCode(500);
            }
        }

        /// <summary>GET api/UsoSistema/Conectados?minutos=5</summary>
        [HttpGet("Conectados")]
        public async Task<IActionResult> Conectados([FromQuery] int minutos = 5)
        {
            if (!TienePermisoModoDesarrollador()) return Forbid();
            return Ok(await _servicio.ConectadosAsync(Math.Clamp(minutos, 1, 60)));
        }

        /// <summary>GET api/UsoSistema/Resumen?desde=2026-10-01&amp;hasta=2026-10-31&amp;empresa=</summary>
        [HttpGet("Resumen")]
        public async Task<IActionResult> Resumen([FromQuery] DateTime desde, [FromQuery] DateTime hasta, [FromQuery] string empresa = null)
        {
            if (!TienePermisoModoDesarrollador()) return Forbid();
            if (hasta < desde) return BadRequest("La fecha final no puede ser anterior a la inicial.");
            return Ok(await _servicio.ResumenAsync(desde, hasta, empresa));
        }

        /// <summary>GET api/UsoSistema/Detalle?tipo=P&amp;ruta=/home/facturacion/consulta&amp;desde=...&amp;hasta=...: usuarios de una pantalla o boton.</summary>
        [HttpGet("Detalle")]
        public async Task<IActionResult> Detalle([FromQuery] string tipo, [FromQuery] string ruta, [FromQuery] string detalle,
            [FromQuery] DateTime desde, [FromQuery] DateTime hasta, [FromQuery] string empresa = null)
        {
            if (!TienePermisoModoDesarrollador()) return Forbid();
            if (hasta < desde) return BadRequest("La fecha final no puede ser anterior a la inicial.");
            return Ok(await _servicio.DetalleAsync(tipo, ruta, detalle, desde, hasta, empresa));
        }

        /// <summary>GET api/UsoSistema/Empresas</summary>
        [HttpGet("Empresas")]
        public async Task<IActionResult> Empresas()
        {
            if (!TienePermisoModoDesarrollador()) return Forbid();
            return Ok(await _servicio.EmpresasAsync());
        }

        /// <summary>GET api/UsoSistema/PrimerRegistro</summary>
        [HttpGet("PrimerRegistro")]
        public async Task<IActionResult> PrimerRegistro()
        {
            if (!TienePermisoModoDesarrollador()) return Forbid();
            return Ok(new { fecha = await _servicio.PrimerRegistroAsync() });
        }

        private string EmpresaDelUsuario() =>
            User.Claims.FirstOrDefault(c => c.Type == "NombreEmpresaMostrar")?.Value
            ?? User.Claims.FirstOrDefault(c => c.Type == "Empresa")?.Value;

        private bool TienePermisoModoDesarrollador() =>
            User.Claims.Any(c =>
                (c.Type == "role" || c.Type == System.Security.Claims.ClaimTypes.Role) &&
                c.Value == "ModoDesarrollador");
    }
}
