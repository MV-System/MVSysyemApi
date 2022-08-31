using Microsoft.AspNetCore.Mvc;
using MVSystemApi.Interfaz;
using MVSystemApi.Model;
using Rotativa.AspNetCore;

namespace MVSystemApi.Controllers
{
    [ApiControllerAttribute]
    public class FacturacionReporteController : Controller
    {
        private readonly IAccesoDatos _accesoDatos;

        public FacturacionReporteController(IAccesoDatos accesoDatos)
        {
            _accesoDatos = accesoDatos;
        }

        [HttpPost]
        [Route("FacturacionReporte/PostFactura")]
        public IActionResult Index(Facturas factura)
        {
            var result = _accesoDatos.PostFactura(factura);
            if (result == null)
                return NotFound();

            return new ViewAsPdf(result);
        }
    }
}
