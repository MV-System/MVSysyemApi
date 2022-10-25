using DTO;
using Microsoft.AspNetCore.Authorization;
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
        public IActionResult ReporteEquiposDisponibles(EquipoReporte equipoReporte)
        {

            equipoReporte.EquipoFilter.PageSize =int.MaxValue ;

            equipoReporte.EquipoDisponibles = ((List<EquipoDisponibleDTO>)_equipos_Negocio.GetEquiposDisponible(equipoReporte.EquipoFilter));

        
            if (equipoReporte == null)
                return NotFound();
            return new ViewAsPdf(equipoReporte);
        }     
        [HttpPost]
        [Route("EquipoVendidoReporte/GetEquiposVendidos")]
        public IActionResult ReportEquipoVendido(EquipoVendidoReporte equipoReporte)
        {
            equipoReporte.EquipoFilter.PageSize = int.MaxValue;
            equipoReporte.EquipoVendidos = (List<EquipoVendido>)_equipos_Negocio.GetEquiposVendidos(equipoReporte.EquipoFilter);

            if (equipoReporte == null)
                return NotFound();
            return new ViewAsPdf(equipoReporte);
        }


        [HttpGet]
        [Route("GetEquiposRecibidoVistaReporte")]
        [Authorize(Roles = "MNU_MANT_EQUIPOS")]
        public IActionResult ReporteEquiposRecibidos([FromQuery]CriterioFilters filterData)
        {
            filterData.PageSize = int.MaxValue;
            List<EquipoRecepcionGet> equiposRecibidos = (List<EquipoRecepcionGet>)_equipos_Negocio.GetEquiposRecibidos(filterData);

            if (equiposRecibidos == null)
                return NotFound();
            return new ViewAsPdf(new ReporteData() { Registros = equiposRecibidos, FechaImpresion = DateTime.Today.ToString() });
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
