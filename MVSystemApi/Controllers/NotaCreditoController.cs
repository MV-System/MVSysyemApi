using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MVSystemApi.Interfaz;
using MVSystemApi.Model;
using MVSystemApi.Model_Negocio;

namespace MVSystemApi.Controllers
{
    /// <summary>
    /// Endpoint para emitir Notas de Crédito que anulan comprobantes electrónicos
    /// emitidos a través de Proveedor 2 (MicroStar ECFManager).
    /// Por ahora solo soporta anulación total (COD_MODIFICACION = 1).
    /// </summary>
    [Route("NotaCredito")]
    [ApiController]
    public class NotaCreditoController : ControllerBase
    {
        private readonly Facturas_Negocio AD;
        private readonly IAccesoDatos _accesoDatos;
        private readonly ILogger<NotaCreditoController> _logger;

        public NotaCreditoController(Facturas_Negocio _en, IAccesoDatos accesoDatos, ILogger<NotaCreditoController> logger)
        {
            AD = _en;
            _accesoDatos = accesoDatos;
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
    }
}
