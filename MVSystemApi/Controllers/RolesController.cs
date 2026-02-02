using DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MVSystemApi.Model_Negocio.Seguridad;

namespace MVSystemApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RolesController : ControllerBase
    {
        private readonly PerfilesService _rolesService;
        private readonly ILogger<RolesController> _logger;

        public RolesController(PerfilesService rolesService, ILogger<RolesController> logger)
        {
            _rolesService = rolesService;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene todos los roles del sistema
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<RolResponseDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ObtenerTodos()
        {
            try
            {
                var roles = await _rolesService.ObtenerTodosPerfiles();
                return Ok(roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener roles");
                return StatusCode(StatusCodes.Status500InternalServerError, new { mensaje = "Error al obtener roles", error = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene un rol específico
        /// </summary>
        [HttpGet("{rolId}")]
        [ProducesResponseType(typeof(RolResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ObtenerPorId(int rolId)
        {
            try
            {
                var rol = await _rolesService.ObtenerPerfilPorCodigo(rolId);
                if (rol == null)
                    return NotFound(new { mensaje = "Rol no encontrado" });

                return Ok(rol);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener rol");
                return StatusCode(StatusCodes.Status500InternalServerError, new { mensaje = "Error al obtener rol", error = ex.Message });
            }
        }

        /// <summary>
        /// Crea un nuevo rol
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Crear([FromBody] CrearRolDTO rolDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var rolId = await _rolesService.CrearPerfil(rolDTO);
                return CreatedAtAction(nameof(ObtenerPorId), new { rolId }, 
                    new { mensaje = "Rol creado exitosamente", rolId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear rol");
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        /// <summary>
        /// Actualiza un rol
        /// </summary>
        [HttpPut("{rolId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Actualizar(int rolId, [FromBody] ActualizarRolDTO rolDTO)
        {
            try
            {
                rolDTO.Codigo = rolId;

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var resultado = await _rolesService.ActualizarPerfil(rolDTO);
                if (!resultado)
                    return NotFound(new { mensaje = "Rol no encontrado" });

                return Ok(new { mensaje = "Rol actualizado exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar rol");
                return StatusCode(StatusCodes.Status500InternalServerError, new { mensaje = "Error al actualizar rol", error = ex.Message });
            }
        }

        /// <summary>
        /// Asigna permisos a un rol
        /// </summary>
        [HttpPost("{rolId}/permisos")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AsignarPermisos(int rolId, [FromBody] List<int> permisosIds)
        {
            try
            {
                if (permisosIds == null || permisosIds.Count == 0)
                    return BadRequest(new { mensaje = "Debe seleccionar al menos un permiso" });

                var resultado = await _rolesService.AsignarPermisosAlPerfil(rolId, permisosIds);
                return Ok(new { mensaje = "Permisos asignados exitosamente al rol" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al asignar permisos al rol");
                return StatusCode(StatusCodes.Status500InternalServerError, new { mensaje = "Error al asignar permisos", error = ex.Message });
            }
        }

        /// <summary>
        /// Elimina un rol
        /// </summary>
        [HttpDelete("{rolId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Eliminar(int rolId)
        {
            try
            {
                var resultado = await _rolesService.EliminarPerfil(rolId);
                if (!resultado)
                    return NotFound(new { mensaje = "Rol no encontrado" });

                return Ok(new { mensaje = "Rol eliminado exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar rol");
                return StatusCode(StatusCodes.Status500InternalServerError, new { mensaje = "Error al eliminar rol", error = ex.Message });
            }
        }
    }
}
