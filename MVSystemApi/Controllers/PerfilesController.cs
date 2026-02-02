using DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MVSystemApi.Model_Negocio.Seguridad;

namespace MVSystemApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PerfilesController : ControllerBase
    {
        private readonly RolesService _rolesService;
        private readonly ILogger<PerfilesController> _logger;

        public PerfilesController(RolesService rolesService, ILogger<PerfilesController> logger)
        {
            _rolesService = rolesService;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene todos los perfiles del sistema
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<PerfilDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ObtenerTodos()
        {
            try
            {
                var perfiles = await _rolesService.ObtenerTodosPerfiles();
                return Ok(perfiles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener perfiles");
                return StatusCode(StatusCodes.Status500InternalServerError, new { mensaje = "Error al obtener perfiles", error = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene un perfil específico
        /// </summary>
        [HttpGet("{codigoPerfil}")]
        [ProducesResponseType(typeof(PerfilDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ObtenerPorCodigo(int codigoPerfil)
        {
            try
            {
                var perfil = await _rolesService.ObtenerPerfilPorCodigo(codigoPerfil);
                if (perfil == null)
                    return NotFound(new { mensaje = "Perfil no encontrado" });

                return Ok(perfil);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener perfil");
                return StatusCode(StatusCodes.Status500InternalServerError, new { mensaje = "Error al obtener perfil", error = ex.Message });
            }
        }
    }
}
