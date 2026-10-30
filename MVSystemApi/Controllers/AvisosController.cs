using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MVSystemApi.Model;
using MVSystemApi.Services;

namespace MVSystemApi.Controllers
{
    /// <summary>
    /// Avisos y novedades. Pendientes y Vistos los usa cualquier usuario autenticado;
    /// crear, listar, activar/desactivar y ver quien lo vio es solo para el permiso ModoDesarrollador.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AvisosController : ControllerBase
    {
        private static readonly string[] Tipos = { "NOVEDAD", "AVISO" };
        private static readonly string[] Prioridades = { "N", "U" };
        private static readonly string[] Destinos = { "TODOS", "EMPRESA", "USUARIOS" };

        private readonly AvisosService _servicio;
        private readonly ILogger<AvisosController> _logger;

        public AvisosController(AvisosService servicio, ILogger<AvisosController> logger)
        {
            _servicio = servicio;
            _logger = logger;
        }

        /// <summary>GET api/Avisos/Pendientes: avisos vigentes que el usuario aun no ha visto.</summary>
        [HttpGet("Pendientes")]
        public async Task<IActionResult> Pendientes()
        {
            try
            {
                var permisos = new HashSet<string>(
                    User.Claims
                        .Where(c => c.Type == "role" || c.Type == System.Security.Claims.ClaimTypes.Role)
                        .Select(c => c.Value),
                    StringComparer.OrdinalIgnoreCase);

                return Ok(await _servicio.PendientesAsync(User.Identity?.Name, EmpresaDelUsuario(), permisos));
            }
            catch (Exception ex)
            {
                // Consultar avisos nunca debe afectar el trabajo del usuario
                _logger.LogError(ex, "No se pudieron consultar los avisos pendientes");
                return Ok(new List<AvisoDto>());
            }
        }

        /// <summary>POST api/Avisos/Vistos: marca avisos como vistos por el usuario.</summary>
        [HttpPost("Vistos")]
        public async Task<IActionResult> Vistos([FromBody] AvisosVistosDto dto)
        {
            if (dto?.Ids == null || dto.Ids.Count == 0) return BadRequest();
            try
            {
                await _servicio.MarcarVistosAsync(dto.Ids, User.Identity?.Name);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudieron marcar los avisos como vistos");
                return StatusCode(500);
            }
        }

        /// <summary>GET api/Avisos: todos los avisos (administracion).</summary>
        [HttpGet]
        public async Task<IActionResult> Listar()
        {
            if (!TienePermisoModoDesarrollador()) return Forbid();
            return Ok(await _servicio.ListarAsync());
        }

        /// <summary>POST api/Avisos: crea un aviso o novedad (administracion).</summary>
        [HttpPost]
        public async Task<IActionResult> Crear([FromBody] AvisoNuevoDto dto)
        {
            if (!TienePermisoModoDesarrollador()) return Forbid();

            var error = Validar(dto);
            if (error != null) return BadRequest(error);

            dto.Titulo = dto.Titulo.Trim();
            dto.Mensaje = dto.Mensaje.Trim();
            var id = await _servicio.CrearAsync(dto, User.Identity?.Name);
            return Ok(new { idAviso = id });
        }

        /// <summary>POST api/Avisos/{id}/Activo?activo=false: activa o desactiva un aviso (administracion).</summary>
        [HttpPost("{id:int}/Activo")]
        public async Task<IActionResult> CambiarActivo(int id, [FromQuery] bool activo)
        {
            if (!TienePermisoModoDesarrollador()) return Forbid();
            return await _servicio.CambiarActivoAsync(id, activo) ? Ok() : NotFound();
        }

        /// <summary>GET api/Avisos/{id}/Vistos: quien vio el aviso y cuando (administracion).</summary>
        [HttpGet("{id:int}/Vistos")]
        public async Task<IActionResult> QuienLoVio(int id)
        {
            if (!TienePermisoModoDesarrollador()) return Forbid();
            return Ok(await _servicio.VistosAsync(id));
        }

        private static string Validar(AvisoNuevoDto dto)
        {
            if (dto == null) return "Datos invalidos.";
            if (!Tipos.Contains(dto.Tipo)) return "El tipo debe ser NOVEDAD o AVISO.";
            if (!Prioridades.Contains(dto.Prioridad)) return "La prioridad debe ser N (normal) o U (urgente).";
            if (!Destinos.Contains(dto.Destino)) return "El destino debe ser TODOS, EMPRESA o USUARIOS.";
            if (string.IsNullOrWhiteSpace(dto.Titulo) || dto.Titulo.Length > 150) return "El titulo es obligatorio (maximo 150 caracteres).";
            if (string.IsNullOrWhiteSpace(dto.Mensaje) || dto.Mensaje.Length > 1000) return "El mensaje es obligatorio (maximo 1000 caracteres).";
            if (dto.Destino != "TODOS" && string.IsNullOrWhiteSpace(dto.DestinoValor)) return "Indique la empresa o los usuarios a quienes va dirigido.";
            if (dto.FechaVence.HasValue && dto.FechaInicio.HasValue && dto.FechaVence <= dto.FechaInicio) return "El vencimiento debe ser posterior al inicio.";
            return null;
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
