using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MVSystemApi.Interfaz;
using MVSystemApi.Model;
using MVSystemApi.Model_Negocio;
using MVSystemApi.Model_Negocio.Seguridad;
using Rotativa.AspNetCore;

namespace MVSystemApi.Controllers
{
    /// <summary>
    /// Endpoints para emitir Notas de Crédito que anulan comprobantes electrónicos.
    /// Proveedor 1 (EasyConexión) y proveedor 2 (MicroStar ECFManager).
    /// Solo soporta anulación total (COD_MODIFICACION = 1 / modifReasonId = 1).
    /// </summary>
    [Route("NotaCredito")]
    [ApiController]
    public class NotaCreditoController : Controller
    {
        private readonly Facturas_Negocio AD;
        private readonly IAccesoDatos _accesoDatos;
        private readonly SeguridadService _seguridadService;
        private readonly IFacEDGIService _facEDGIService;

        public NotaCreditoController(Facturas_Negocio _en, IAccesoDatos accesoDatos, SeguridadService seguridadService, IFacEDGIService facEDGIService)
        {
            AD = _en;
            _accesoDatos = accesoDatos;
            _seguridadService = seguridadService;
            _facEDGIService = facEDGIService;
        }

        /// <summary>El usuario sale solo del token (no del cuerpo de la solicitud).</summary>
        private string UsuarioToken() => (User?.Identity?.Name ?? User?.FindFirst("name")?.Value)?.Trim();

        /// <summary>
        /// Emite la nota de crédito total que anula una factura con e-NCF y espera la respuesta de la DGII (hasta 60 s).
        /// Con la nota aprobada, anula la factura (solo si sigue activa). Respuesta: { ncf, estado, anulada, mensaje }.
        /// POST: NotaCredito/PostNotaCredito
        /// </summary>
        [HttpPost]
        [Route("PostNotaCredito")]
        public async Task<ActionResult> PostNotaCredito([FromBody] NotaCreditoProveedor2Request request)
        {
            var idProveedorClaim = User?.FindFirst("IdProveedor")?.Value ?? "1";
            int.TryParse(idProveedorClaim, out var idProveedor);
            if (idProveedor == 2)
                return await PostNotaCreditoProveedor2(request);

            try
            {
                if (request == null || request.NumeroFacturaOrigen <= 0)
                    return BadRequest(new { error = "Indique la factura a anular." });

                var usuario = UsuarioToken();
                if (string.IsNullOrWhiteSpace(usuario))
                    return Unauthorized(new { error = "No se pudo identificar al usuario de la sesión." });

                var sucursal = int.TryParse(request.IDsucursal, out var idSuc) ? idSuc : 0;
                var rnc = User?.FindFirst("Rnc")?.Value ?? "";
                var empresa = User?.FindFirst("Empresa")?.Value ?? "";

                var resultado = await _facEDGIService.EmitirNotaCreditoAsync(request.NumeroFacturaOrigen, sucursal, usuario, rnc, empresa);
                return Ok(resultado);
            }
            catch (NotaCreditoException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Nota de crédito del proveedor 2 (MicroStar). Mismas reglas que el proveedor 1: idempotente por factura, candado por
        /// factura, log con literal "N" + literal real, y anulación de la factura solo cuando la DGII acepta la nota.
        /// Lo que se inserta en PIVOT_FACTURAS_ENC / _DET para el proveedor no cambia.
        /// </summary>
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

                if (request == null || request.NumeroFacturaOrigen <= 0)
                    return BadRequest(new { error = "Indique la factura a anular." });

                var usuario = UsuarioToken();
                if (string.IsNullOrWhiteSpace(usuario))
                    return Unauthorized(new { error = "No se pudo identificar al usuario de la sesión." });

                var numero = request.NumeroFacturaOrigen;
                var sucursal = int.TryParse(request.IDsucursal, out var idSuc) ? idSuc : 0;
                var rncClaim = User?.FindFirst("Rnc")?.Value ?? "";

                // Candado en la base: dos solicitudes simultáneas para la misma factura no se procesan a la vez
                await using var candado = await _accesoDatos.TomarCandadoAsync($"NC_{sucursal}_{numero}");
                if (candado == null)
                    return BadRequest(new { error = "Esta factura ya se está procesando." });

                var literal = await _accesoDatos.ObtenerLiteralFactura(numero, sucursal);
                if (string.IsNullOrWhiteSpace(literal))
                    return NotFound(new { error = "Factura origen no encontrada." });
                var literalNc = "N" + literal;

                // 1. Idempotencia: si ya hay NC no se emite otra, aunque la factura ya no esté activa
                var logNc = await _accesoDatos.GetLogFacturaElectronica(numero, literalNc);
                if (logNc != null)
                {
                    var estadoLog = (logNc.Estado ?? "").Trim().ToUpperInvariant();
                    if (estadoLog == "ACEPTADA")
                        return Ok(await ResultadoAprobadaAsync(numero, sucursal, usuario, logNc.VoucherNumber_DGII, "La nota de crédito ya fue aceptada por la DGII."));

                    if (estadoLog != "RECHAZADO_DGII" && estadoLog != "ERROR")
                    {
                        // PEND: consulta puntual del estado real en la tabla remota del proveedor
                        var estatus = await _accesoDatos.ObtenerEstatusRemotoProveedor2(logNc.VoucherNumber_DGII, rncClaim);
                        if (estatus == "ACEPTADO")
                        {
                            logNc.Estado = "ACEPTADA";
                            logNc.Fecha_Confirmacion = DateTime.Now;
                            logNc.Error_Descripcion = null;
                            await _accesoDatos.UpdateLogFacturaElectronica(logNc);
                            return Ok(await ResultadoAprobadaAsync(numero, sucursal, usuario, logNc.VoucherNumber_DGII, "La DGII aceptó la nota de crédito."));
                        }
                        if (estatus == "RECHAZADO_DGII")
                        {
                            logNc.Estado = "RECHAZADO_DGII";
                            await _accesoDatos.UpdateLogFacturaElectronica(logNc);
                            return Ok(new NotaCreditoResultado { Ncf = logNc.VoucherNumber_DGII, Estado = "RECHAZADA", Anulada = false, Mensaje = "La DGII rechazó la nota de crédito. La factura sigue activa." });
                        }
                        return Ok(new NotaCreditoResultado { Ncf = logNc.VoucherNumber_DGII, Estado = "PENDIENTE", Anulada = false, Mensaje = "La nota de crédito sigue pendiente de respuesta de la DGII. Espere un minuto y vuelva a pulsar el botón: no se emite otra nota de crédito, solo se consulta la respuesta." });
                    }
                    // RECHAZADO_DGII o ERROR: la anterior no prosperó; se emite una nueva sobre el mismo registro de log
                }

                // 2. Validaciones para emitir una NC nueva (lecturas filtradas por el RNC del emisor)
                var facturaOrigen = await _accesoDatos.ObtenerFacturaParaNotaCredito(numero, sucursal, rncClaim);
                if (facturaOrigen == null)
                    return NotFound(new { error = "Factura origen no encontrada." });

                var origenActual = await _accesoDatos.ObtenerFacturaOrigenNotaCredito(numero, sucursal);
                if (origenActual == null || !string.Equals(origenActual.Estado, "A", StringComparison.OrdinalIgnoreCase))
                    return BadRequest(new { error = "La factura no está activa; no se puede emitir una nota de crédito." });

                var estatusRemoto = await _accesoDatos.ObtenerEstatusRemotoProveedor2(facturaOrigen.NcfNumero, rncClaim);
                if (estatusRemoto != "ACEPTADO")
                {
                    // Rechazada por la DGII: la factura nunca fue válida, no hace falta una nota de crédito. Se anula directamente.
                    if (estatusRemoto != null && estatusRemoto.StartsWith("RECHAZ", StringComparison.OrdinalIgnoreCase))
                    {
                        var (anuladaDirecta, errorAnulacion) = await _accesoDatos.AnularFacturaPorNotaCreditoAsync(
                            numero, sucursal, usuario, null, "Anulada automáticamente: la DGII rechazó la factura electrónica");
                        if (!anuladaDirecta)
                            return BadRequest(new { error = $"La DGII rechazó esta factura, pero no se pudo anularla: {errorAnulacion}" });
                        return Ok(new NotaCreditoResultado
                        {
                            Ncf = facturaOrigen.NcfNumero,
                            Estado = "ANULADA",
                            Anulada = true,
                            Mensaje = "La DGII había rechazado esta factura. No hace falta nota de crédito: la factura quedó anulada."
                        });
                    }
                    if (string.Equals(estatusRemoto, "ERROR", StringComparison.OrdinalIgnoreCase))
                        return BadRequest(new { error = "El proveedor reporta un error con el comprobante de esta factura y aún no está aceptado por la DGII. Revíselo con el proveedor." });
                    return BadRequest(new { error = "Solo se puede emitir una Nota de Crédito de facturas aceptadas por la DGII." });
                }

                var detallesOrigenObj = AD.GetDetalleFacturaConsulta(numero, sucursal);
                var detallesOrigen = detallesOrigenObj as List<DetalleFactura> ?? new List<DetalleFactura>();
                if (detallesOrigen.Count == 0)
                    return BadRequest(new { error = "La factura origen no tiene detalle para replicar en la Nota de Crédito." });

                var idEmpresaClaim = User?.FindFirst("IdEmpresa")?.Value ?? "1";
                var idSucursalClaim = User?.FindFirst("idSucursal")?.Value ?? request.IDsucursal;

                var ncf = await _accesoDatos.ObtenerNcfNotaCredito(34);
                if (string.IsNullOrWhiteSpace(ncf))
                    return BadRequest(new { error = "No se pudo asignar el e-NCF de la Nota de Crédito." });

                var facturaNC = new Facturas
                {
                    NumeroFactura = numero,
                    IdCliente = facturaOrigen.IdCliente,
                    NcfTipoNumero = 34,
                    NcfNumero = ncf,
                    SubTotal = facturaOrigen.SubTotal,
                    Descuento = facturaOrigen.Descuento,
                    Itbis = facturaOrigen.Itbis,
                    Total = facturaOrigen.Total,
                    RNC = facturaOrigen.RNC,
                    Nota = request.Motivo ?? "",
                    Usuario = usuario,
                    FechaRegistro = DateTime.Now,
                    empresa = "",
                    IdEmpresa = idEmpresaClaim,
                    IdSucursal = idSucursalClaim
                };

                var notaCreditoInfo = new NotaCreditoInfo
                {
                    DocReferencia = numero.ToString(),
                    FechaDocReferencia = facturaOrigen.FechaRegistro,
                    EncfReferencia = facturaOrigen.NcfNumero,   // el de la fila local filtrada por número + sucursal + RNC
                    CodModificacion = 1,
                    LiteralLog = literalNc
                };

                var (ok, errorMsg) = await _accesoDatos.InsertFacturaProveedor2Dual(facturaNC, detallesOrigen, usuario, rncClaim, notaCreditoInfo);

                // 3. El Dual deja el resultado en el log de la NC: ACEPTADA, RECHAZADO_DGII o PEND (sin respuesta, o error propio del proveedor)
                var logResultado = await _accesoDatos.GetLogFacturaElectronica(numero, literalNc);
                var estadoResultado = (logResultado?.Estado ?? "").Trim().ToUpperInvariant();

                if (estadoResultado == "ACEPTADA")
                    return Ok(await ResultadoAprobadaAsync(numero, sucursal, usuario, ncf, "La DGII aceptó la nota de crédito."));

                if (estadoResultado == "RECHAZADO_DGII")
                    return Ok(new NotaCreditoResultado
                    {
                        Ncf = ncf,
                        Estado = "RECHAZADA",
                        Anulada = false,
                        Mensaje = $"La DGII rechazó la nota de crédito. La factura sigue activa. {errorMsg}".Trim()
                    });

                if (!ok)
                    return BadRequest(new { error = errorMsg ?? "No se pudo registrar la Nota de Crédito en Proveedor 2." });

                return Ok(new NotaCreditoResultado
                {
                    Ncf = ncf,
                    Estado = "PENDIENTE",
                    Anulada = false,
                    Mensaje = string.IsNullOrWhiteSpace(errorMsg)
                        ? "Nota de crédito enviada; pendiente de respuesta de la DGII. Espere un minuto y vuelva a pulsar el botón: no se emite otra nota de crédito, solo se consulta la respuesta."
                        : $"El proveedor no pudo procesar el comprobante: {errorMsg}. Se avisó a soporte; reintente más tarde."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>Con la NC aceptada anula la factura (solo si sigue activa) y arma la respuesta.</summary>
        private async Task<NotaCreditoResultado> ResultadoAprobadaAsync(int numero, int sucursal, string usuario, string ncf, string mensajeBase)
        {
            var (anulada, error) = await _accesoDatos.AnularFacturaPorNotaCreditoAsync(numero, sucursal, usuario, ncf);
            return new NotaCreditoResultado
            {
                Ncf = ncf,
                Estado = "APROBADA",
                Anulada = anulada,
                Mensaje = anulada
                    ? $"{mensajeBase} La factura quedó anulada."
                    : $"{mensajeBase} Pero no se pudo anular la factura: {error}. Vuelva a pulsar el botón para reintentar la anulación."
            };
        }

        [HttpPost]
        [Route("ReportNotaCredito")]
        public async Task<IActionResult> ReportNotaCredito([FromBody] NotaCreditoProveedor2Request request)
        {
            var sucursal = int.TryParse(request.IDsucursal, out var idSuc) ? idSuc : 0;
            var rncClaim = User?.FindFirst("Rnc")?.Value ?? "";

            // La NC se identifica por el log nuevo (literal "N" + literal real): su e-NCF es la llave del encabezado y del detalle
            var literal = await _accesoDatos.ObtenerLiteralFactura(request.NumeroFacturaOrigen, sucursal);
            var log = string.IsNullOrWhiteSpace(literal) ? null : await _accesoDatos.GetLogFacturaElectronica(request.NumeroFacturaOrigen, "N" + literal);
            if (log == null || string.IsNullOrWhiteSpace(log.VoucherNumber_DGII))
                return NotFound(new { error = "Nota de crédito anterior: consúltela en el portal del proveedor." });

            var data = await _accesoDatos.ObtenerNotaCreditoReporte(request.NumeroFacturaOrigen, sucursal, rncClaim, log.VoucherNumber_DGII.Trim());
            if (data == null)
                return NotFound(new { error = "Nota de Crédito no encontrada." });

            var idEmpresaClaim = User?.FindFirst("IdEmpresa")?.Value ?? "1";
            data.DatosEmpresaFactura = _seguridadService.GetDatosEmpresa(Convert.ToInt32(idEmpresaClaim), sucursal);

            string baseUrl = $"{Request.Scheme}://{Request.Host.Value}";
            data.DatosEmpresaFactura.Logo = baseUrl + "/assets/" + data.DatosEmpresaFactura.Logo + ".png";

            if (log.QR_Url != null)
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
