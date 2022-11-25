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
            return Ok(_seguridadService.ChangePassword(usuario));
        }
    }
}
