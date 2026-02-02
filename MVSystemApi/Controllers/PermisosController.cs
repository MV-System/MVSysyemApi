using DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MVSystemApi.Model_Negocio.Seguridad;

namespace MVSystemApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PermisosController : ControllerBase
    {
        private readonly PermisosService _permisosService;
        private readonly ILogger<PermisosController> _logger;

        public PermisosController(PermisosService permisosService, ILogger<PermisosController> logger)
        {
            _permisosService = permisosService;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene todos los permisos del sistema
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<PermisoResponseDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ObtenerTodos()
        {
            try
            {
                var permisos = await _permisosService.ObtenerTodosPermisos();
                return Ok(permisos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener permisos");
                return StatusCode(StatusCodes.Status500InternalServerError, new { mensaje = "Error al obtener permisos", error = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene permisos de un módulo específico
        /// </summary>
        [HttpGet("modulo/{codigoModulo}")]
        [ProducesResponseType(typeof(List<PermisoResponseDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ObtenerPorModulo(int codigoModulo)
        {
            try
            {
                var permisos = await _permisosService.ObtenerPermisosPorModulo(codigoModulo);
                return Ok(permisos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener permisos por módulo");
                return StatusCode(StatusCodes.Status500InternalServerError, new { mensaje = "Error al obtener permisos", error = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene un permiso específico
        /// </summary>
        [HttpGet("{permisoId}")]
        [ProducesResponseType(typeof(PermisoResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ObtenerPorId(int permisoId)
        {
            try
            {
                var permiso = await _permisosService.ObtenerPermisoPorCodigo(permisoId);
                if (permiso == null)
                    return NotFound(new { mensaje = "Permiso no encontrado" });

                return Ok(permiso);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener permiso");
                return StatusCode(StatusCodes.Status500InternalServerError, new { mensaje = "Error al obtener permiso", error = ex.Message });
            }
        }

        /// <summary>
        /// Crea un nuevo permiso
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Crear([FromBody] CrearPermisoDTO permisoDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var permisoId = await _permisosService.CrearPermiso(permisoDTO);
                return CreatedAtAction(nameof(ObtenerPorId), new { permisoId }, 
                    new { mensaje = "Permiso creado exitosamente", permisoId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear permiso");
                return BadRequest(new { mensaje = ex.Message });
            }
        }

        /// <summary>
        /// Actualiza un permiso
        /// </summary>
        [HttpPut("{permisoId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Actualizar(int permisoId, [FromBody] ActualizarPermisoDTO permisoDTO)
        {
            try
            {
                permisoDTO.Codigo = permisoId;

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var resultado = await _permisosService.ActualizarPermiso(permisoDTO);
                if (!resultado)
                    return NotFound(new { mensaje = "Permiso no encontrado" });

                return Ok(new { mensaje = "Permiso actualizado exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar permiso");
                return StatusCode(StatusCodes.Status500InternalServerError, new { mensaje = "Error al actualizar permiso", error = ex.Message });
            }
        }

        /// <summary>
        /// Elimina un permiso
        /// </summary>
        [HttpDelete("{permisoId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Eliminar(int permisoId)
        {
            try
            {
                var resultado = await _permisosService.EliminarPermiso(permisoId);
                if (!resultado)
                    return NotFound(new { mensaje = "Permiso no encontrado" });

                return Ok(new { mensaje = "Permiso eliminado exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar permiso");
                return StatusCode(StatusCodes.Status500InternalServerError, new { mensaje = "Error al eliminar permiso", error = ex.Message });
            }
        }
    }
}
