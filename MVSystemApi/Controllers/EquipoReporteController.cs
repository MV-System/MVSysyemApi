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
        private readonly Accesorios_Negocio _AccesorioNegocios;
        public EquipoReporteController(Equipos_Negocio equipos_Negocio, Accesorios_Negocio accesorioNegocios)
        {
            _equipos_Negocio = equipos_Negocio;
            _AccesorioNegocios = accesorioNegocios;
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


        [HttpGet]
        [Route("GetEquiposRecibidoVistaReporte")]
        public IActionResult ReporteEquiposRecibidos([FromQuery]CriterioFilters filterData)
        {
            filterData.PageSize = int.MaxValue;
            List<EquipoRecepcionGet> equiposRecibidos = (List<EquipoRecepcionGet>)_equipos_Negocio.GetEquiposRecibidos(filterData);

            if (equiposRecibidos == null)
                return NotFound();
            return new ViewAsPdf(new ReporteData() { Registros = equiposRecibidos, FechaImpresion = DateTime.Now.ToString("ddd, dd MMM yyyy") });
        }


        [HttpGet]
        [Route("EquiposInventarioVistaReporte")]
        public IActionResult ReporteEquiposInventario([FromQuery] InventarioFilters filterData)
        {
            filterData.PageSize = int.MaxValue;
            List<EquipoInventarioResponse> equipos = (List<EquipoInventarioResponse>)_equipos_Negocio.GetEquipos(filterData);
            if (equipos == null)
                return NotFound();
            return new ViewAsPdf(new ReporteEquiposInventario() { Registros = equipos, FechaImpresion = DateTime.Now.ToString("ddd, dd MMM yyyy") });
        }

        [HttpGet]
        [Route("AccesorioInventarioVistaReporte")]
        public IActionResult ReporteAccesoriosInventario([FromQuery] InventarioFilters filterData)
        {
            filterData.PageSize = int.MaxValue;
            List<AccesorioInventarioResponse> accesorios = (List<AccesorioInventarioResponse>)_AccesorioNegocios.GetAccesorios(filterData);
            if (accesorios == null)
                return NotFound();
            return new ViewAsPdf(new ReporteAccesoriosInventario() { Registros = accesorios, FechaImpresion = DateTime.Now.ToString("ddd, dd MMM yyyy") });
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
