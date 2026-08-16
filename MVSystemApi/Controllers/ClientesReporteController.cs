using DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MVSystemApi.Interfaz;
using MVSystemApi.Model;
using MVSystemApi.Model_Negocio;
using MVSystemApi.Model_Negocio.Seguridad;
using Rotativa.AspNetCore;

namespace MVSystemApi.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ClientesReporteController : Controller
    {
        private readonly IAccesoDatos _accesoDatos;
        private readonly Clientes_Negocio _clientesNegocio;
        private readonly SeguridadService _SS;
        private readonly ILogger<ClientesReporteController> _logger;

        public ClientesReporteController(IAccesoDatos accesoDatos, Clientes_Negocio clientesNegocio, SeguridadService SS, ILogger<ClientesReporteController> logger)
        {
            _accesoDatos = accesoDatos;
            _clientesNegocio = clientesNegocio;
            _SS = SS;
            _logger = logger;
        }

        [HttpGet]
        [Route("ReciboAbono")]
        public IActionResult ReciboAbono(int idCliente, int numeroFactura, int idSucursal, int idEmpresa)
        {
            try
            {
                var recibo = _accesoDatos.GetReciboAbono(idCliente, numeroFactura, idSucursal);
                if (recibo == null)
                {
                    return NotFound("No se encontró recibo de abono para los filtros enviados.");
                }

                DatosEmpresa datosEmpresa = _SS.GetDatosEmpresa(idEmpresa, idSucursal);
                if (datosEmpresa == null)
                {
                    return BadRequest("No se pudieron obtener los datos de empresa para generar el recibo.");
                }

                string baseUrl = $"{Request.Scheme}://{Request.Host.Value}";
                recibo.Logo = baseUrl + "/assets/" + datosEmpresa.Logo + ".png";

                var data = new EmpresaReportes
                {
                    DatosEmpresaFactura = datosEmpresa,
                    ReciboAbono = new List<ReciboAbonoDTO> { recibo }
                };

                return new ViewAsPdf(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando ReciboAbono. idCliente={IdCliente}, numeroFactura={NumeroFactura}, idSucursal={IdSucursal}, idEmpresa={IdEmpresa}",
                    idCliente, numeroFactura, idSucursal, idEmpresa);

                return StatusCode(500, $"Error generando ReciboAbono: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("ReciboAbonoGeneral")]
        public IActionResult ReciboAbonoGeneral(Guid idLoteAbono, int idSucursal, int idEmpresa)
        {
            try
            {
                var recibo = _accesoDatos.GetReciboAbonoGeneral(idLoteAbono);
                if (recibo == null)
                {
                    return NotFound("No se encontró recibo de abono general para el lote enviado.");
                }

                DatosEmpresa datosEmpresa = _SS.GetDatosEmpresa(idEmpresa, idSucursal);
                if (datosEmpresa == null)
                {
                    return BadRequest("No se pudieron obtener los datos de empresa para generar el recibo.");
                }

                string baseUrl = $"{Request.Scheme}://{Request.Host.Value}";
                recibo.Logo = baseUrl + "/assets/" + datosEmpresa.Logo + ".png";

                var data = new EmpresaReportes
                {
                    DatosEmpresaFactura = datosEmpresa,
                    ReciboAbonoGeneral = new List<ReciboAbonoGeneralDTO> { recibo }
                };

                return new ViewAsPdf(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando ReciboAbonoGeneral. idLoteAbono={IdLoteAbono}, idSucursal={IdSucursal}, idEmpresa={IdEmpresa}",
                    idLoteAbono, idSucursal, idEmpresa);

                return StatusCode(500, $"Error generando ReciboAbonoGeneral: {ex.Message}");
            }
        }

        [HttpGet]
        [Route("HistorialAbonos")]
        public IActionResult HistorialAbonos(int idCliente, int idSucursal, int idEmpresa, string fechaInicio = null, string fechaFinal = null)
        {
            try
            {
                // Obtener el histórico de abonos del cliente
                var historial = _clientesNegocio.ObtenerHistorialAbonos(idCliente, fechaInicio, fechaFinal);
                if (historial == null || historial.Count == 0)
                {
                    return NotFound("No se encontró historial de abonos para el cliente.");
                }

                // Obtener los datos del cliente
                var cliente = _clientesNegocio.Cliente_Consulta_Por_Id_Cliente(idCliente);
                if (cliente == null)
                {
                    return NotFound("No se encontró el cliente.");
                }

                // Obtener los datos de la empresa
                DatosEmpresa datosEmpresa = _SS.GetDatosEmpresa(idEmpresa, idSucursal);
                if (datosEmpresa == null)
                {
                    return BadRequest("No se pudieron obtener los datos de empresa para generar el reporte.");
                }

                string baseUrl = $"{Request.Scheme}://{Request.Host.Value}";
                datosEmpresa.Logo = baseUrl + "/assets/" + datosEmpresa.Logo + ".png";

                var data = new EmpresaReportes
                {
                    DatosEmpresaFactura = datosEmpresa,
                    Cliente = cliente,
                    HistorialAbonos = historial
                };

                return new ViewAsPdf(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando HistorialAbonos. idCliente={IdCliente}, idSucursal={IdSucursal}, idEmpresa={IdEmpresa}",
                    idCliente, idSucursal, idEmpresa);

                return StatusCode(500, $"Error generando HistorialAbonos: {ex.Message}");
            }
        }
    }
}
