using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MVSystemApi.Interfaz;
using MVSystemApi.Model;
using MVSystemApi.Model_Negocio;
using MVSystemApi.Model_Negocio.Seguridad;
using Rotativa.AspNetCore;

namespace MVSystemApi.Controllers
{
    /// <summary>
    /// Endpoint para emitir Notas de Crédito que anulan comprobantes electrónicos
    /// emitidos a través de Proveedor 2 (MicroStar ECFManager).
    /// Por ahora solo soporta anulación total (COD_MODIFICACION = 1).
    /// </summary>
    [Route("NotaCredito")]
    [ApiController]
    public class NotaCreditoController : Controller
    {
        private readonly Facturas_Negocio AD;
        private readonly IAccesoDatos _accesoDatos;
        private readonly SeguridadService _seguridadService;
        private readonly ILogger<NotaCreditoController> _logger;

        public NotaCreditoController(Facturas_Negocio _en, IAccesoDatos accesoDatos, SeguridadService seguridadService, ILogger<NotaCreditoController> logger)
        {
            AD = _en;
            _accesoDatos = accesoDatos;
            _seguridadService = seguridadService;
            _logger = logger;
        }

        [HttpPost]
        [Route("PostNotaCreditoProveedor2")]
        public async Task<ActionResult> PostNotaCreditoProveedor2([FromBody] NotaCreditoProveedor2Request request)
        {
            try
            {
                var idProveedorClaim = User?.FindFirst("IdProveedor")?.Value ?? "1";
                int.TryParse(idProveedorClaim, out var idProveedor);
                if (idProveedor != 2)
                    return BadRequest(new { error = "La Nota de Crédito por este medio solo aplica para empresas con Proveedor 2 (MicroStar)." });

                var sucursal = int.TryParse(request.IDsucursal, out var idSuc) ? idSuc : 0;

                var facturaOrigen = await _accesoDatos.ObtenerFacturaParaNotaCredito(request.NumeroFacturaOrigen, sucursal);
                if (facturaOrigen == null)
                    return NotFound(new { error = "Factura origen no encontrada." });

                var rncClaim = User?.FindFirst("Rnc")?.Value ?? "";

                var estatusRemoto = await _accesoDatos.ObtenerEstatusRemotoProveedor2(facturaOrigen.NcfNumero, rncClaim);
                if (estatusRemoto != "ACEPTADO")
                    return BadRequest(new { error = "Solo se puede emitir una Nota de Crédito de facturas aceptadas por la DGII." });

                var detallesOrigenObj = AD.GetDetalleFacturaConsulta(request.NumeroFacturaOrigen, sucursal);
                var detallesOrigen = detallesOrigenObj as List<DetalleFactura> ?? new List<DetalleFactura>();
                if (detallesOrigen.Count == 0)
                    return BadRequest(new { error = "La factura origen no tiene detalle para replicar en la Nota de Crédito." });

                var logOrigen = await _accesoDatos.GetLogFacturaElectronica(request.NumeroFacturaOrigen, "A");

                var idEmpresaClaim = User?.FindFirst("IdEmpresa")?.Value ?? "1";
                var idSucursalClaim = User?.FindFirst("idSucursal")?.Value ?? request.IDsucursal;

                var ncf = await _accesoDatos.ObtenerNcfNotaCredito(34);
                if (string.IsNullOrWhiteSpace(ncf))
                    return BadRequest(new { error = "No se pudo asignar el e-NCF de la Nota de Crédito." });

                var facturaNC = new Facturas
                {
                    NumeroFactura = request.NumeroFacturaOrigen,
                    IdCliente = facturaOrigen.IdCliente,
                    NcfTipoNumero = 34,
                    NcfNumero = ncf,
                    SubTotal = facturaOrigen.SubTotal,
                    Descuento = facturaOrigen.Descuento,
                    Itbis = facturaOrigen.Itbis,
                    Total = facturaOrigen.Total,
                    RNC = facturaOrigen.RNC,
                    Nota = request.Motivo ?? "",
                    Usuario = request.Usuario ?? "Sistema",
                    FechaRegistro = DateTime.Now,
                    empresa = "",
                    IdEmpresa = idEmpresaClaim,
                    IdSucursal = idSucursalClaim
                };

                var notaCreditoInfo = new NotaCreditoInfo
                {
                    DocReferencia = request.NumeroFacturaOrigen.ToString(),
                    FechaDocReferencia = facturaOrigen.FechaRegistro,
                    EncfReferencia = logOrigen?.VoucherNumber_DGII ?? facturaOrigen.NcfNumero,
                    CodModificacion = 1
                };

                var (ok, errorMsg) = await _accesoDatos.InsertFacturaProveedor2Dual(facturaNC, detallesOrigen, request.Usuario ?? "Sistema", rncClaim, notaCreditoInfo);

                if (!ok)
                    return BadRequest(new { error = errorMsg ?? "No se pudo registrar la Nota de Crédito en Proveedor 2." });

                return Ok(new
                {
                    numeroFactura = facturaNC.NumeroFactura,
                    ncf = facturaNC.NcfNumero,
                    mensaje = "Nota de Crédito registrada exitosamente"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar Nota de Crédito Proveedor 2");
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost]
        [Route("ReportNotaCredito")]
        public async Task<IActionResult> ReportNotaCredito([FromBody] NotaCreditoProveedor2Request request)
        {
            var sucursal = int.TryParse(request.IDsucursal, out var idSuc) ? idSuc : 0;

            var data = await _accesoDatos.ObtenerNotaCreditoReporte(request.NumeroFacturaOrigen, sucursal);
            if (data == null)
                return NotFound(new { error = "Nota de Crédito no encontrada." });

            var idEmpresaClaim = User?.FindFirst("IdEmpresa")?.Value ?? "1";
            data.DatosEmpresaFactura = _seguridadService.GetDatosEmpresa(Convert.ToInt32(idEmpresaClaim), sucursal);

            string baseUrl = $"{Request.Scheme}://{Request.Host.Value}";
            data.DatosEmpresaFactura.Logo = baseUrl + "/assets/" + data.DatosEmpresaFactura.Logo + ".png";

            var log = await _accesoDatos.GetLogFacturaElectronica(request.NumeroFacturaOrigen, "NC");
            if (log?.QR_Url != null)
            {
                data.QrUrl = log.QR_Url;
                var (fechaFirma, codigoSeguridad) = ExtractQrParameters(log.QR_Url);
                data.FechaFirma = fechaFirma;
                data.CodigoSeguridad = codigoSeguridad;
            }

            return new ViewAsPdf(data);
        }

        private (string FechaFirma, string CodigoSeguridad) ExtractQrParameters(string qrUrl)
        {
            if (string.IsNullOrEmpty(qrUrl))
                return (null, null);

            try
            {
                var uri = new Uri(qrUrl);
                var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);

                string fechaFirma = queryParams["FechaFirma"];
                string codigoSeguridad = queryParams["CodigoSeguridad"];

                return (fechaFirma, codigoSeguridad);
            }
            catch
            {
                return (null, null);
            }
        }
    }
}
