using Microsoft.AspNetCore.Mvc;
using MVSystemApi.Interfaz;

namespace MVSystemApi.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IRoleService _roleService;

        public UserController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        [HttpGet(nameof(GetPermisos))]
        public async Task<ActionResult> GetPermisos() => Ok(await _roleService.GetPermisos());
    }
}
