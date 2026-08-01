using DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MVSystemApi.Interfaz;
using MVSystemApi.Model;
using MVSystemApi.Model_Negocio.Seguridad;
using Rotativa.AspNetCore;

namespace MVSystemApi.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ClientesReporteController : Controller
    {
        private readonly IAccesoDatos _accesoDatos;
        private readonly SeguridadService _SS;
        private readonly ILogger<ClientesReporteController> _logger;

        public ClientesReporteController(IAccesoDatos accesoDatos, SeguridadService SS, ILogger<ClientesReporteController> logger)
        {
            _accesoDatos = accesoDatos;
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
    }
}
