using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MVSystemApi.Model;

namespace MVSystemApi.Controllers
{
    [Route("Vendedores")]
    [ApiController]
    public class VendedoresController : ControllerBase
    {
        Vendedores_Negocio VN;

        public VendedoresController(Vendedores_Negocio _vn)
        {
            VN = _vn;
        }

        [HttpPost()]
        [Route("Vendedor_Insert")]
        [Authorize("MNU_MANT_VENDEDORES")]
        public ActionResult<Vendedor> Vendedor_Insert([FromBody] Vendedor Vendedor)
        {
            try
            {
                var result = VN.Vendedor_Insert(Vendedor);
                if (result == null)
                {
                    return NotFound(StatusCodes.Status200OK);
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
               return BadRequest(ex.Message);
            }
        }
    }
}
