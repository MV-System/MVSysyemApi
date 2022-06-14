using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MVSystemApi.Model;
using MVSystemApi.Model_Negocio;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace MVSystemApi.Controllers
{
    [Route("Factura")]
    [ApiController]
    public class FacturacionController : Controller
    {
        private readonly Facturas_Negocio AD;
        public FacturacionController(Facturas_Negocio _en)
        {
            AD = _en;
        }
        [HttpPost]
        [Route("post_Factura")]
        public ActionResult<Facturas> post_Factura([FromBody]Facturas factura)
        {
            try
            {

                var result = AD.Post_Factura(factura,0,null);
                if (result == null)
                {
                    return NotFound();
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
