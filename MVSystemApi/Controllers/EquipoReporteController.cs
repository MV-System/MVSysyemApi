using DTO;
using Microsoft.AspNetCore.Mvc;
using MVSystemApi.Model;
using Rotativa.AspNetCore;

namespace MVSystemApi.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class EquipoReporteController : ControllerBase
    {
        private readonly Equipos_Negocio _equipos_Negocio;
        public EquipoReporteController(Equipos_Negocio equipos_Negocio)
        {
            _equipos_Negocio = equipos_Negocio;
        }

        [HttpPost]
        [Route("EquipoDisponibleReporte/GetEquiposDisponible")]
        public IActionResult ReporteEquiposDisponibles(EquipoDisponibleFilterDTO equipoReporte)
        {

            equipoReporte.PageSize =int.MaxValue ;

            var equipoDisponibles = ((List<EquipoDisponibleDTO>)_equipos_Negocio.GetEquiposDisponible(equipoReporte));

        
            if (equipoDisponibles == null)
                return NotFound();
            return new ViewAsPdf(equipoDisponibles);
        }     
        [HttpPost]
        [Route("EquipoVendidoReporte/GetEquiposVendidos")]
        public IActionResult ReportEquipoVendido(EquipoVendidoFilter equipoReporte)
        {
            equipoReporte.PageSize = int.MaxValue;
           var equipoVendidos = (List<EquipoVendido>)_equipos_Negocio.GetEquiposVendidos(equipoReporte);

            if (equipoVendidos == null)
                return NotFound();
            return new ViewAsPdf(equipoVendidos);
        }


        [HttpPost]
        [Route("EquipoTransferenciaReporte")]
        public IActionResult EquipoTransferenciaReporte(EquipoTranferenciaReporte equipoReporte)
        {
            equipoReporte.EquipoTransferencias = (List<EquipoTransferencia>)_equipos_Negocio.GetEquiposTranferidos();
            if (equipoReporte == null)
                return NotFound();
            return new ViewAsPdf(equipoReporte);
        }
    }
}
