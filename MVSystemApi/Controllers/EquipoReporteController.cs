using DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MVSystemApi.Interfaz;
using MVSystemApi.Model;
using MVSystemApi.Model_Negocio.Seguridad;
using Rotativa.AspNetCore;
using System.Security.Claims;

namespace MVSystemApi.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class EquipoReporteController : ControllerBase
    {
        private readonly Equipos_Negocio _equipos_Negocio;
        private readonly SeguridadService _SS;
        private readonly AccesoDatos _accesoDatos;
        private readonly int _laptopsHorasMaxCustodia;

        public EquipoReporteController(Equipos_Negocio equipos_Negocio, SeguridadService SS, AccesoDatos accesoDatos, IConfiguration configuration)
        {
            _laptopsHorasMaxCustodia = configuration.GetValue("Laptops:HorasMaxCustodia", 24);
            _equipos_Negocio = equipos_Negocio;
            _SS = SS;
            _accesoDatos = accesoDatos;
        }

        /// <summary>
        /// Comprobante (ticket de 80 mm) de la pieza retirada que queda en custodia del técnico.
        /// GET: EquipoReporte/ComprobanteCustodia?idCambio=&idEmpresa=&idSucursal=&tecnicoNombre=
        /// </summary>
        [HttpGet]
        [Authorize]
        [Route("ComprobanteCustodia")]
        public IActionResult ComprobanteCustodia(int idCambio, int idEmpresa, int idSucursal, string tecnicoNombre = null)
        {
            try
            {
                // Mismos permisos que la pantalla de cambio de pieza y la consulta de custodia
                bool autorizado = User.Claims.Any(claim =>
                    (claim.Type == "role" || claim.Type == ClaimTypes.Role)
                    && (claim.Value == "LAPTOP_CAMBIO_PIEZA" || claim.Value == "LAPTOP_CUSTODIA_CONSULTA"));
                if (!autorizado)
                {
                    return StatusCode(StatusCodes.Status403Forbidden, "No tiene permisos para realizar esta acción.");
                }

                var tabla = _accesoDatos.Equipo_Cambio_Pieza_Comprobante(idCambio);
                if (tabla == null || tabla.Rows.Count == 0)
                {
                    return NotFound("No se encontró el cambio de pieza.");
                }

                var fila = tabla.Rows[0];
                string Texto(string columna) => fila[columna] == DBNull.Value ? null : Convert.ToString(fila[columna]);

                var fechaRegistro = Convert.ToDateTime(fila["Fecha_Registro"]);
                var comprobante = new ComprobanteCustodiaDTO
                {
                    IdCambio = Convert.ToInt32(fila["ID_Cambio"]),
                    FechaRegistro = fechaRegistro,
                    HorasMax = _laptopsHorasMaxCustodia,
                    FechaLimite = fechaRegistro.AddHours(_laptopsHorasMaxCustodia),
                    Imei = Texto("Imei"),
                    Marca = Texto("Marca"),
                    Modelo = Texto("Modelo"),
                    TipoPieza = Texto("Tipo_Pieza"),
                    PiezaRetirada = Texto("Pieza_Retirada"),
                    ModeloRetirada = Texto("Modelo_Retirada"),
                    PiezaInstalada = Texto("Pieza_Instalada"),
                    ModeloInstalada = Texto("Modelo_Instalada"),
                    ConfigAnterior = Texto("Config_Anterior"),
                    ConfigNueva = Texto("Config_Nueva"),
                    CostoRetirado = fila["Costo_Retirado"] == DBNull.Value ? 0 : Convert.ToDecimal(fila["Costo_Retirado"]),
                    Tecnico = Texto("Tecnico"),
                    TecnicoNombre = tecnicoNombre,
                    UsuarioRegistro = Texto("Usuario_Registro"),
                    EstadoCustodia = Texto("Estado_Custodia"),
                    Almacen = Texto("Descripcion_Almacen")
                };

                if (comprobante.EstadoCustodia != "PENDIENTE")
                {
                    return BadRequest("Solo las piezas en custodia (pendientes de recibir) tienen comprobante.");
                }

                DatosEmpresa datosEmpresa = _SS.GetDatosEmpresa(idEmpresa, idSucursal);
                if (datosEmpresa == null)
                {
                    return BadRequest("No se pudieron obtener los datos de empresa para generar el comprobante.");
                }

                string baseUrl = $"{Request.Scheme}://{Request.Host.Value}";
                datosEmpresa.Logo = baseUrl + "/assets/" + datosEmpresa.Logo + ".png";

                var data = new EmpresaReportes
                {
                    DatosEmpresaFactura = datosEmpresa,
                    ComprobanteCustodia = comprobante
                };

                return new ViewAsPdf(data);
            }
            catch (Exception)
            {
                return StatusCode(500, "Se produjo un error al generar el comprobante de custodia.");
            }
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
                DatosEmpresa DatosEmpresaFactura = _SS.GetDatosEmpresa(Convert.ToInt32(equipoReporte.IdEmpresa),Convert.ToInt32(equipoReporte.IdSucursal));
                
                string baseUrl = $"{Request.Scheme}://{Request.Host.Value}"; // Obtener la URL base del servidor
                equipoDisponibles.ForEach(ft => ft.logo = baseUrl + "/assets/" + DatosEmpresaFactura.Logo + ".png");
               
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
            DatosEmpresa DatosEmpresaFactura = _SS.GetDatosEmpresa(Convert.ToInt32(equipoReporte.IdEmpresa), Convert.ToInt32(equipoReporte.IdSucursal));

            string baseUrl = $"{Request.Scheme}://{Request.Host.Value}"; // Obtener la URL base del servidor
            equipoVendidos.ForEach(ft => ft.logo = baseUrl + "/assets/" + DatosEmpresaFactura.Logo + ".png");

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

            var EquipoReporte = (List<EquipoTransferencia>)_equipos_Negocio.GetEquiposTranferidos(equipoReporte.IdTransferencia);
            if (EquipoReporte == null)
                return NotFound();
            
            // Obtener los datos de la empresa (factura) con el IdEmpresa que llega en el request
            DatosEmpresa DatosEmpresaFactura = _SS.GetDatosEmpresa(Convert.ToInt32(equipoReporte.IdEmpresa), Convert.ToInt32(equipoReporte.IdSucursal));

            string baseUrl = $"{Request.Scheme}://{Request.Host.Value}"; // Obtener la URL base del servidor
            EquipoReporte.ForEach(ft => ft.Logo = baseUrl + "/assets/" + DatosEmpresaFactura.Logo + ".png");

            // Crear el objeto EmpresaFactura para la vista
            var data = new EmpresaReportes
            {
                DatosEmpresaFactura = DatosEmpresaFactura,
                EquipoTransferencia = EquipoReporte,
            };

            return new ViewAsPdf(data);
        }

        [HttpGet]
        [Route("ReporteReparaciones")]
        public IActionResult ReporteReparaciones(int idReparacion, int idEmpresa, int idSucursal)
        {
            // Obtener la reparación (como lista para mantener la estructura)
            var reparacion = _accesoDatos.ObtenerReparacionReporte(idReparacion);
            if (reparacion == null)
                return NotFound();

            // Obtener los datos de la empresa
            DatosEmpresa datosEmpresa = _SS.GetDatosEmpresa(idEmpresa, idSucursal);

            // Agregar el logo a la reparación
            string baseUrl = $"{Request.Scheme}://{Request.Host.Value}";
            reparacion.Logo = baseUrl + "/assets/" + datosEmpresa.Logo + ".png";

            // Construir el objeto para la vista
            var data = new EmpresaReportes
            {
                DatosEmpresaFactura = datosEmpresa,
                Reparaciones = new List<ReparacionReporteDTO> { reparacion }
            };

            // Retornar la vista PDF
            return new ViewAsPdf(data);
        }

        [HttpGet]
        [Route("ReporteEquiposRegistrados")]
        public IActionResult ReporteEquiposRegistrados(int numeroRegistro, int idEmpresa, int idSucursal)
        {
            try
            {
                var equiposRegistrados = _equipos_Negocio.GetEquiposRegistrados(numeroRegistro);

                if (equiposRegistrados == null || equiposRegistrados.Count == 0)
                    return NotFound("No se encontraron equipos para este número de registro.");

                DatosEmpresa datosEmpresa = _SS.GetDatosEmpresa(idEmpresa, idSucursal);

                string baseUrl = $"{Request.Scheme}://{Request.Host.Value}";
                equiposRegistrados.ForEach(e => e.Logo = baseUrl + "/assets/" + datosEmpresa.Logo + ".png");

                var data = new EmpresaReportes
                {
                    DatosEmpresaFactura = datosEmpresa,
                    EquiposRegistrados = equiposRegistrados
                };

                return new ViewAsPdf("ReporteEquiposRegistrados", data);
            }
            catch (Exception)
            {
                return StatusCode(500, "Se produjo un error al generar el reporte de equipos registrados.");
            }
        }
    }
}
