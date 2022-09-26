using DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MVSystemApi.Model_Negocio.Seguridad;

namespace MVSystemApi.Controllers
{
    [Route("login")]
    [ApiController]
    [AllowAnonymous]
    public class LoginController : ControllerBase
    {
        private readonly SeguridadService _seguridadService;

        public LoginController(SeguridadService seguridadService)
        {
            _seguridadService = seguridadService;
        }

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody] UsuarioDTO usuario)
        {
            return Ok(await _seguridadService.Login(usuario));
        }
    }
}
