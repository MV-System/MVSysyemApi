using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MVSystemApi.Model;

namespace MVSystemApi.Controllers
{
    [Route("Proveedores")]
    [ApiController]
    public class ProveedoresController : ControllerBase
    {
        private readonly Proveedores_Negocio PN;

        public ProveedoresController(Proveedores_Negocio _pn)
        {
            PN = _pn;
        }

        [HttpPost]
        [Route("Proveedor_Insert")]
        [Authorize("MNU_MANT_SUPLIDORES")]
        public ActionResult Proveedor_Insert(Suplidor Suplidor)
        {
            try
            {
                var lista = PN.Suplidor_Insert(Suplidor);
                if (lista == null)
                {
                    return NotFound();
                }
                return Ok(lista);
            }
            catch (Exception ex)
            {

                return BadRequest(ex.Message);
            }

        }
    }
}
