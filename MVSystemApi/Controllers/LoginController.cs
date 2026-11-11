using DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MVSystemApi.Interfaz;
using MVSystemApi.Model;
using MVSystemApi.Model.Seguridad;
using MVSystemApi.Handlers;
using MVSystemApi.Model_Negocio.Seguridad;

namespace MVSystemApi.Controllers
{
    [Route("login")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly SeguridadService _seguridadService;
        private readonly LimiteIntentosLogin _limiteIntentos;

        public LoginController(SeguridadService seguridadService, LimiteIntentosLogin limiteIntentos)
        {
            _seguridadService = seguridadService;
            _limiteIntentos = limiteIntentos;
        }

        [HttpPost]
        [Route("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] UsuarioDTO usuario)
        {
            var ip = Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim() ?? HttpContext.Connection.RemoteIpAddress?.ToString();
            var login = usuario?.UserName;

            if (_limiteIntentos.EstaBloqueado(login, ip, out var minutos))
                return StatusCode(StatusCodes.Status429TooManyRequests,
                    new { error = $"Demasiados intentos. Intente en {minutos} minuto(s)." });

            var credenciales = await _seguridadService.Login(usuario);
            if (credenciales == null)
            {
                var (fallos, restantes) = _limiteIntentos.RegistrarFallo(login, ip);
                var mensaje = "Credenciales incorrectas.";
                if (restantes == 0)
                    mensaje += $" Su usuario fue bloqueado por {LimiteIntentosLogin.MinutosDeBloqueo} minutos.";
                else if (fallos >= 2)
                    mensaje += $" Le quedan {restantes} intento(s) antes de que su usuario sea bloqueado.";
                // 200 sin token: el front lo trata como credenciales incorrectas y muestra este mensaje
                return Ok(new { token = (string)null, expires = (DateTime?)null, mensaje });
            }

            _limiteIntentos.RegistrarExito(login);
            return Ok(credenciales);
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
