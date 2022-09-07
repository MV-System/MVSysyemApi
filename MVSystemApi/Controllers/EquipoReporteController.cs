using DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MVSystemApi.Interfaz;
using MVSystemApi.Model;
using Rotativa.AspNetCore;

namespace MVSystemApi.Controllers
{
    //[Route("[controller]")]
    [ApiController]
    public class EquipoReporteController : ControllerBase
    {

        [HttpPost]
        [Route("EquipoDisponibleReporte/GetEquiposDisponible")]
        public IActionResult Index(EquipoReporte equipoReporte)
        {
            if (equipoReporte == null)
                return NotFound();
            return new ViewAsPdf(equipoReporte);
        }     
        [HttpPost]
        [Route("EquipoVendidoReporte/GetEquiposVendidos")]
        public IActionResult ReportEquipoVendido(EquipoVendidoReporte equipoReporte)
        {
            if (equipoReporte == null)
                return NotFound();
            return new ViewAsPdf(equipoReporte);
        }

    }
}
