using DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MVSystemApi.Model_Negocio.Seguridad;

namespace MVSystemApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AsignacionesController : ControllerBase
    {
        private readonly AsignacionesService _asignacionesService;
        private readonly ILogger<AsignacionesController> _logger;

        public AsignacionesController(AsignacionesService asignacionesService, ILogger<AsignacionesController> logger)
        {
            _asignacionesService = asignacionesService;
            _logger = logger;
        }

        /// <summary>
        /// Modifica los permisos de un usuario (agrega y elimina según sea necesario)
        /// </summary>
        [HttpPut("modificar-permisos")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ModificarPermisosUsuario([FromBody] AsignacionPermisoDTO asignacionDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var resultado = await _asignacionesService.ModificarPermisosUsuario(asignacionDTO);
                return Ok(new { mensaje = "Permisos modificados exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al modificar permisos");
                return StatusCode(StatusCodes.Status500InternalServerError, new { mensaje = "Error al modificar permisos", error = ex.Message });
            }
        }

        /// <summary>
        /// Asigna permisos a un usuario
        /// </summary>
        [HttpPost("asignar-permisos")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AsignarPermisosUsuario([FromBody] AsignacionPermisoDTO asignacionDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var resultado = await _asignacionesService.AsignarPermisosUsuario(asignacionDTO);
                return Ok(new { mensaje = "Permisos asignados exitosamente al usuario" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al asignar permisos");
                return StatusCode(StatusCodes.Status500InternalServerError, new { mensaje = "Error al asignar permisos", error = ex.Message });
            }
        }

        /// <summary>
        /// Desasigna un permiso de un usuario
        /// </summary>
        [HttpPost("desasignar-permiso")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DesasignarPermisoUsuario([FromBody] DesasignacionPermisoDTO desasignacionDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var resultado = await _asignacionesService.DesasignarPermisoUsuario(desasignacionDTO);
                if (!resultado)
                    return BadRequest(new { mensaje = "No se encontró el permiso asignado a este usuario. El permiso puede no estar asignado o ya haber sido removido." });

                return Ok(new { mensaje = "Permiso desasignado exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al desasignar permiso");
                return StatusCode(StatusCodes.Status500InternalServerError, new { mensaje = "Error al desasignar permiso", error = ex.Message });
            }
        }

        /// <summary>
        /// Asigna un perfil/rol a un usuario
        /// </summary>
        [HttpPost("perfil")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AsignarPerfilUsuario([FromBody] AsignacionPerfilDTO asignacionDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var resultado = await _asignacionesService.AsignarPerfilUsuario(asignacionDTO);
                return Ok(new { mensaje = "Perfil asignado exitosamente al usuario" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al asignar perfil");
                return StatusCode(StatusCodes.Status500InternalServerError, new { mensaje = "Error al asignar perfil", error = ex.Message });
            }
        }

        /// <summary>
        /// Desasigna un perfil/rol de un usuario
        /// </summary>
        [HttpDelete("perfil")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DesasignarPerfilUsuario([FromBody] DesasignacionPerfilDTO desasignacionDTO)
        {
            try
            {
                var resultado = await _asignacionesService.DesasignarPerfilUsuario(desasignacionDTO);
                if (!resultado)
                    return BadRequest(new { mensaje = "No se pudo desasignar el perfil" });

                return Ok(new { mensaje = "Perfil desasignado exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al desasignar perfil");
                return StatusCode(StatusCodes.Status500InternalServerError, new { mensaje = "Error al desasignar perfil", error = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene los permisos efectivos de un usuario (directos + heredados de roles) con datos completos
        /// </summary>
        [HttpGet("permisos-efectivos/{codigoUsuario}")]
        [ProducesResponseType(typeof(List<Dictionary<string, object>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ObtenerPermisosEfectivos(int codigoUsuario)
        {
            try
            {
                var permisos = await _asignacionesService.ObtenerPermisosEfectivosConDatos(codigoUsuario);
                return Ok(permisos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener permisos efectivos");
                return StatusCode(StatusCodes.Status500InternalServerError, new { mensaje = "Error al obtener permisos", error = ex.Message });
            }
        }

        /// <summary>
        /// Verifica si un usuario tiene un permiso específico
        /// </summary>
        [HttpGet("usuario/{codigoUsuario}/tiene-permiso/{permisoId}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> VerificarPermiso(int codigoUsuario, int permisoId)
        {
            try
            {
                var tienePermiso = await _asignacionesService.UsuarioTienePermiso(codigoUsuario, permisoId);
                return Ok(new { codigoUsuario, permisoId, tienePermiso });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar permiso");
                return StatusCode(StatusCodes.Status500InternalServerError, new { mensaje = "Error al verificar permiso", error = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene el perfil/rol asignado a un usuario con sus permisos
        /// </summary>
        [HttpGet("usuario/{codigoUsuario}/perfil")]
        [ProducesResponseType(typeof(PerfilResponseDTO), StatusCodes.Status200OK)]
        public async Task<IActionResult> ObtenerPerfilUsuario(int codigoUsuario)
        {
            try
            {
                var perfil = await _asignacionesService.ObtenerPerfilUsuario(codigoUsuario);
                return Ok(perfil);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener perfil del usuario");
                return StatusCode(StatusCodes.Status500InternalServerError, new { mensaje = "Error al obtener perfil", error = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene los permisos directos de un usuario (sin heredados) con datos completos
        /// </summary>
        [HttpGet("permisos-directos/{codigoUsuario}")]
        [ProducesResponseType(typeof(List<Dictionary<string, object>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ObtenerPermisosDirectos(int codigoUsuario)
        {
            try
            {
                var permisos = await _asignacionesService.ObtenerPermisosDirectosConDatos(codigoUsuario);
                return Ok(permisos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener permisos directos");
                return StatusCode(StatusCodes.Status500InternalServerError, new { mensaje = "Error al obtener permisos", error = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene los permisos heredados de un usuario (del perfil asignado) con datos completos
        /// </summary>
        [HttpGet("permisos-heredados/{codigoUsuario}")]
        [ProducesResponseType(typeof(List<Dictionary<string, object>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ObtenerPermisosHeredados(int codigoUsuario)
        {
            try
            {
                var permisos = await _asignacionesService.ObtenerPermisosHeredadosConDatos(codigoUsuario);
                return Ok(permisos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener permisos heredados");
                return StatusCode(StatusCodes.Status500InternalServerError, new { mensaje = "Error al obtener permisos", error = ex.Message });
            }
        }
    }
}
