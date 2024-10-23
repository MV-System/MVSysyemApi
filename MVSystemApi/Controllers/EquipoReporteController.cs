using DTO;
using Microsoft.AspNetCore.Mvc;
using MVSystemApi.Model;
using MVSystemApi.Model_Negocio.Seguridad;
using Rotativa.AspNetCore;

namespace MVSystemApi.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class EquipoReporteController : ControllerBase
    {
        private readonly Equipos_Negocio _equipos_Negocio;
        private readonly SeguridadService _SS;

        public EquipoReporteController(Equipos_Negocio equipos_Negocio, SeguridadService SS)
        {
            _equipos_Negocio = equipos_Negocio;
            _SS = SS;
        }

        [HttpPost]
        [Route("EquipoDisponibleReporte/GetEquiposDisponible")]
        public IActionResult ReporteEquiposDisponibles(EquipoDisponibleFilterDTO equipoReporte)
        {
            try
            {
                equipoReporte.PageSize = int.MaxValue;

                // Obtener los equipos disponibles
                var equipoDisponibles = (List<EquipoDisponibleDTO>)_equipos_Negocio.GetEquiposDisponible(equipoReporte);
                if (equipoDisponibles == null || equipoDisponibles.Count == 0)
                    return NotFound();

                // Obtener los datos de la empresa (factura) con el IdEmpresa que llega en el request
                DatosEmpresa DatosEmpresaFactura = _SS.GetDatosEmpresa(Convert.ToInt32(equipoReporte.IdEmpresa));
                
                string baseUrl = $"{Request.Scheme}://{Request.Host.Value}"; // Obtener la URL base del servidor
                equipoDisponibles.ForEach(ft => ft.logo = baseUrl + "/assets/" + DatosEmpresaFactura.Empresa + ".png");
               
                // Crear el objeto EmpresaFactura para la vista
                var data = new EmpresaReportes
                {
                    DatosEmpresaFactura = DatosEmpresaFactura,
                    EquiposDTO = equipoDisponibles,
                };

                // Devolver la vista PDF con el objeto modificado
                return new ViewAsPdf(data);
            }
            catch (Exception)
            {
                return StatusCode(500, "Se produjo un error al generar el reporte.");
            }
        }
        [HttpPost]
        [Route("EquipoVendidoReporte/GetEquiposVendidos")]
        public IActionResult ReportEquipoVendido(EquipoVendidoFilter equipoReporte)
        {
            equipoReporte.PageSize = int.MaxValue;
            var equipoVendidos = (List<EquipoVendido>)_equipos_Negocio.GetEquiposVendidos(equipoReporte);

            if (equipoVendidos == null)
                return NotFound();

            // Obtener los datos de la empresa (factura) con el IdEmpresa que llega en el request
            DatosEmpresa DatosEmpresaFactura = _SS.GetDatosEmpresa(Convert.ToInt32(equipoReporte.IdEmpresa));

            string baseUrl = $"{Request.Scheme}://{Request.Host.Value}"; // Obtener la URL base del servidor
            equipoVendidos.ForEach(ft => ft.logo = baseUrl + "/assets/" + DatosEmpresaFactura.Empresa + ".png");

            // Crear el objeto EmpresaFactura para la vista
            var data = new EmpresaReportes
            {
                DatosEmpresaFactura = DatosEmpresaFactura,
                EquipoVendido = equipoVendidos,
            };

            return new ViewAsPdf(data);
        }


        [HttpGet]
        [Route("GetEquiposRecibidoVistaReporte")]
        //[Authorize(Roles = "MNU_MANT_EQUIPOS")]
        public IActionResult ReporteEquiposRecibidos([FromQuery]CriterioFilters filterData)
        {
            filterData.PageSize = int.MaxValue;
            List<EquipoRecepcionGet> equiposRecibidos = (List<EquipoRecepcionGet>)_equipos_Negocio.GetEquiposRecibidos(filterData);

            if (equiposRecibidos == null)
                return NotFound();
            return new ViewAsPdf(new ReporteData() { Registros = equiposRecibidos, FechaImpresion = DateTime.Now.ToString("ddd, dd MMM yyyy") });
        }

        [HttpPost]
        [Route("EquipoTransferenciaReporte")]
        public IActionResult EquipoTransferenciaReporte(EquipoTransferencia equipoReporte)
        {

            var EquipoReporte = (List<EquipoTransferencia>)_equipos_Negocio.GetEquiposTranferidos();
            if (EquipoReporte == null)
                return NotFound();
            
            // Obtener los datos de la empresa (factura) con el IdEmpresa que llega en el request
            DatosEmpresa DatosEmpresaFactura = _SS.GetDatosEmpresa(Convert.ToInt32(equipoReporte.IdEmpresa));

            string baseUrl = $"{Request.Scheme}://{Request.Host.Value}"; // Obtener la URL base del servidor
            EquipoReporte.ForEach(ft => ft.Logo = baseUrl + "/assets/" + DatosEmpresaFactura.Empresa + ".png");

            // Crear el objeto EmpresaFactura para la vista
            var data = new EmpresaReportes
            {
                DatosEmpresaFactura = DatosEmpresaFactura,
                EquipoTransferencia = EquipoReporte,
            };

            return new ViewAsPdf(data);
        }
    }
}
