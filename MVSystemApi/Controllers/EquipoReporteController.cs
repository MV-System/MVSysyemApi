using DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MVSystemApi.Model;
using MVSystemApi.Model_Negocio.Seguridad;
using Rotativa.AspNetCore;
using System.Data;
using System.Reflection.Metadata;

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
                var DatosEmpresaFactura = _SS.GetDatosEmpresa(Convert.ToInt32(equipoReporte.IdEmpresa));

                DatosEmpresa DEF = new DatosEmpresa();

                // Verificar si se encontraron datos de la empresa
                if (DatosEmpresaFactura != null && DatosEmpresaFactura.Rows.Count > 0)
                {
                    var row = DatosEmpresaFactura.Rows[0];
                    DEF.IdEmpresa = Convert.ToInt32(row["ID_Empresa"]);
                    DEF.Empresa = row["Nombre_Empresa"].ToString();
                    DEF.Direccion = row["Direccion"].ToString();
                    DEF.Telefonos = row["Telefonos"].ToString();
                    DEF.Encabezado = row["Encabezado"].ToString();
                    DEF.CondicionesGarantia = row["Condiciones_Garantia"].ToString();
                    DEF.RNC = row["RNC"].ToString();
                }

                // Asignar un valor al parámetro "logo" de cada equipo disponible
                foreach (var equipo in equipoDisponibles)
                {
                    equipo.logo = "/assets/" + DEF.Empresa + ".png"; // Aquí se usa el nombre de la empresa desde DatosEmpresa
                }

                // Crear el objeto EmpresaFactura para la vista
                var data = new EmpresaFactura
                {
                    DatosEmpresaFactura = DEF,
                    EquiposDTO = equipoDisponibles,
                };

                // Devolver la vista PDF con el objeto modificado
                return new ViewAsPdf(data);
            }
            catch (Exception ex)
            {
                // Registrar el error y devolver una respuesta adecuada
                // Log.Error(ex, "Error al generar el reporte de equipos disponibles");
                return StatusCode(500, "Se produjo un error al generar el reporte.");
            }
        }
        [HttpPost]
        [Route("EquipoVendidoReporte/GetEquiposVendidos")]
        public IActionResult ReportEquipoVendido(EquipoVendidoFilter equipoReporte)
        {
            equipoReporte.PageSize = int.MaxValue;
            var equipoVendidos = (List<EquipoVendido>)_equipos_Negocio.GetEquiposVendidos(equipoReporte);

            string baseUrl = $"{Request.Scheme}://{Request.Host.Value}"; // Obtener la URL base del servidor
            equipoVendidos.ForEach(ft => ft.logo = baseUrl + "/assets/" + equipoReporte.Empresa + ".png");
            
            if (equipoVendidos == null)
                return NotFound();
            return new ViewAsPdf(equipoVendidos);
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
        public IActionResult EquipoTransferenciaReporte(EquipoTranferenciaReporte equipoReporte)
        {

            equipoReporte.EquipoTransferencias = (List<EquipoTransferencia>)_equipos_Negocio.GetEquiposTranferidos();
            if (equipoReporte == null)
                return NotFound();

            string baseUrl = $"{Request.Scheme}://{Request.Host.Value}"; // Obtener la URL base del servidor
            equipoReporte.Empresa = (baseUrl + "/assets/" + equipoReporte.Empresa + ".png");

            return new ViewAsPdf(equipoReporte);
        }
    }
}
