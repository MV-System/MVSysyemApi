using DTO;
using Microsoft.AspNetCore.Mvc;
using MVSystemApi.Model_Negocio;

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
        public async Task<IActionResult> Cliente_Insert([FromBody] UsuarioDTO usuario)
        {
            return Ok(await _seguridadService.Login(usuario));
        }
    }
}
