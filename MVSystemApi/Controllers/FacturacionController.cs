using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MVSystemApi.Interfaz;
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
        private readonly IAccesoDatos _accesoDatos;
        public FacturacionController(Facturas_Negocio _en, IAccesoDatos accesoDatos)
        {
            _accesoDatos = accesoDatos;
            AD = _en;
        }
        [HttpPost]
        [Route("PostFactura")]
        public ActionResult<Facturas> PostFactura(Facturas factura)
        {
            try
            {
                var result = _accesoDatos.PostFactura(factura);
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
