using DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MVSystemApi.Model_Negocio.Seguridad;
using System.IdentityModel.Tokens.Jwt;
using MVSystemApi.Handlers;

namespace MVSystemApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class UsuariosController : ControllerBase
    {
        private readonly UsuariosService _usuariosService;
        private readonly ILogger<UsuariosController> _logger;

        public UsuariosController(UsuariosService usuariosService, ILogger<UsuariosController> logger)
        {
            _usuariosService = usuariosService;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene todos los usuarios del sistema
        /// </summary>
        [RequierePermiso(PermisosApi.VerUsuarios, PermisosApi.AsignarPermisos, PermisosApi.LaptopCambioPieza, PermisosApi.LaptopCustodiaConsulta, PermisosApi.LaptopCustodiaRecibir)]
        [HttpGet]
        [ProducesResponseType(typeof(List<UsuarioDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ObtenerTodos()
        {
            try
            {
                var usuarios = await _usuariosService.ObtenerTodosUsuarios();
                return Ok(usuarios);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuarios");
                return StatusCode(StatusCodes.Status500InternalServerError, new { mensaje = "Error al obtener usuarios", error = ErroresApi.Mensaje(ex) });
            }
        }

        /// <summary>
        /// Obtiene un usuario específico con sus permisos y roles
        /// </summary>
        [RequierePermiso(PermisosApi.VerUsuarios)]
        [HttpGet("{codigoUsuario}")]
        [ProducesResponseType(typeof(UsuarioConPermisosDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ObtenerPorCodigo(int codigoUsuario)
        {
            try
            {
                var usuario = await _usuariosService.ObtenerUsuarioPorCodigo(codigoUsuario);
                if (usuario == null)
                    return NotFound(new { mensaje = "Usuario no encontrado" });

                return Ok(usuario);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener usuario");
                return StatusCode(StatusCodes.Status500InternalServerError, new { mensaje = "Error al obtener usuario", error = ErroresApi.Mensaje(ex) });
            }
        }

        /// <summary>
        /// Crea un nuevo usuario
        /// </summary>
        [RequierePermiso(PermisosApi.VerUsuarios)]
        [HttpPost]
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Crear([FromBody] CrearUsuarioDTO usuarioDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var codigoUsuario = await _usuariosService.CrearUsuario(usuarioDTO);
                return CreatedAtAction(nameof(ObtenerPorCodigo), new { codigoUsuario },
                    new { mensaje = "Usuario creado exitosamente", codigoUsuario });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear usuario");
                return BadRequest(new { mensaje = ErroresApi.Mensaje(ex) });
            }
        }

        /// <summary>
        /// Actualiza los datos de un usuario
        /// </summary>
        [RequierePermiso(PermisosApi.VerUsuarios)]
        [HttpPut("{codigoUsuario}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Actualizar(int codigoUsuario, [FromBody] ActualizarUsuarioDTO usuarioDTO)
        {
            try
            {
                usuarioDTO.Codigo = codigoUsuario;

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var resultado = await _usuariosService.ActualizarUsuario(usuarioDTO);
                if (!resultado)
                    return NotFound(new { mensaje = "Usuario no encontrado" });

                return Ok(new { mensaje = "Usuario actualizado exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar usuario");
                return StatusCode(StatusCodes.Status500InternalServerError, new { mensaje = "Error al actualizar usuario", error = ErroresApi.Mensaje(ex) });
            }
        }

        /// <summary>
        /// Cambia la contraseña del usuario actual
        /// </summary>
        [HttpPost("cambiar-contrasena")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CambiarContrasena([FromBody] CambioContrasenaDTO cambioDTO)
        {
            try
            {
                // No registrar el DTO ni los claims: el DTO trae la contraseña actual y la nueva.

                // Obtener el código del usuario del JWT token
                // ASP.NET Core mapea JwtRegisteredClaimNames.NameId a esta URI
                var codigoString = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value 
                    ?? User.FindFirst(JwtRegisteredClaimNames.NameId)?.Value;
                _logger.LogInformation($"[CAMBIAR CONTRASEÑA] NameId del token: {codigoString}");
                
                if (string.IsNullOrEmpty(codigoString) || !int.TryParse(codigoString, out int codigoUsuario))
                    return BadRequest(new { mensaje = "No se pudo obtener la identidad del usuario" });

                cambioDTO.CodigoUsuario = codigoUsuario;

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning($"[CAMBIAR CONTRASEÑA] ModelState inválido: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors))}");
                    return BadRequest(ModelState);
                }

                var resultado = await _usuariosService.CambiarContrasena(cambioDTO);
                if (!resultado)
                    return BadRequest(new { mensaje = "No se pudo cambiar la contraseña" });

                return Ok(new { mensaje = "Contraseña cambiada exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar contraseña");
                return BadRequest(new { mensaje = ErroresApi.Mensaje(ex) });
            }
        }

        /// <summary>
        /// Cambia la contraseña de un usuario (admin)
        /// </summary>
        [RequierePermiso(PermisosApi.VerUsuarios)]
        [HttpPost("{codigoUsuario}/reset-contrasena")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetContrasena(int codigoUsuario, [FromBody] object requestBody)
        {
            try
            {
                // Obtener nueva contraseña del body
                dynamic data = requestBody;
                string nuevaContrasena = data["nuevaContrasena"] ?? throw new Exception("Contraseña requerida");

                if (string.IsNullOrWhiteSpace(nuevaContrasena) || nuevaContrasena.Length < 6)
                    return BadRequest(new { mensaje = "La contraseña debe tener al menos 6 caracteres" });

                var resultado = await _usuariosService.CambiarContrasenaAdmin(codigoUsuario, nuevaContrasena);
                if (!resultado)
                    return BadRequest(new { mensaje = "No se pudo resetear la contraseña" });

                return Ok(new { mensaje = "Contraseña reseteada exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al resetear contraseña");
                return BadRequest(new { mensaje = ErroresApi.Mensaje(ex) });
            }
        }

        /// <summary>
        /// Elimina completamente un usuario (y sus permisos)
        /// </summary>
        [RequierePermiso(PermisosApi.VerUsuarios)]
        [HttpDelete("{codigoUsuario}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Desactivar(int codigoUsuario)
        {
            try
            {
                var resultado = await _usuariosService.EliminarUsuario(codigoUsuario);
                if (!resultado)
                    return NotFound(new { mensaje = "Usuario no encontrado" });

                return Ok(new { mensaje = "Usuario eliminado exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar usuario");
                return StatusCode(StatusCodes.Status500InternalServerError, new { mensaje = "Error al eliminar usuario", error = ErroresApi.Mensaje(ex) });
            }
        }

        /// <summary>
        /// Activa un usuario
        /// </summary>
        [RequierePermiso(PermisosApi.VerUsuarios)]
        [HttpPost("{codigoUsuario}/activar")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Activar(int codigoUsuario)
        {
            try
            {
                var resultado = await _usuariosService.ActivarUsuario(codigoUsuario);
                if (!resultado)
                    return NotFound(new { mensaje = "Usuario no encontrado" });

                return Ok(new { mensaje = "Usuario activado exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al activar usuario");
                return StatusCode(StatusCodes.Status500InternalServerError, new { mensaje = "Error al activar usuario", error = ErroresApi.Mensaje(ex) });
            }
        }

        /// <summary>
        /// Elimina completamente un usuario (y sus relaciones de permisos)
        /// </summary>
        [RequierePermiso(PermisosApi.VerUsuarios)]
        [HttpDelete("{codigoUsuario}/eliminar")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Eliminar(int codigoUsuario)
        {
            try
            {
                var resultado = await _usuariosService.EliminarUsuario(codigoUsuario);
                if (!resultado)
                    return NotFound(new { mensaje = "Usuario no encontrado" });

                return Ok(new { mensaje = "Usuario eliminado exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar usuario");
                return StatusCode(StatusCodes.Status500InternalServerError, new { mensaje = "Error al eliminar usuario", error = ErroresApi.Mensaje(ex) });
            }
        }
    }
}
