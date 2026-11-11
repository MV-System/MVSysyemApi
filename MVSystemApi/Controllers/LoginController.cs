using DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MVSystemApi.Interfaz;
using MVSystemApi.Model;
using MVSystemApi.Model.Seguridad;
using MVSystemApi.Model_Negocio.Seguridad;

namespace MVSystemApi.Controllers
{
    [Route("login")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly SeguridadService _seguridadService;

        public LoginController(SeguridadService seguridadService)
        {
            _seguridadService = seguridadService;
        }

        [HttpPost]
        [Route("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] UsuarioDTO usuario)
        {
            return Ok(await _seguridadService.Login(usuario));
        }


        [HttpPost("ChangePassword")]
        public IActionResult ChangePassword([FromBody] Usuario usuario)
        {
            // Solo cambia la clave del usuario de la sesión: el código sale del token, nunca del body
            // (antes cualquier usuario podía cambiar la clave de otro enviando su código).
            var codigoToken = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.NameId)?.Value;
            if (usuario == null || !int.TryParse(codigoToken, out var codigo))
                return Unauthorized();
            if (string.IsNullOrWhiteSpace(usuario.Password))
                return BadRequest(new { error = "Indique la nueva contraseña." });

            usuario.Codigo = codigo;
            return Ok(_seguridadService.ChangePassword(usuario));
        }
    }
}
