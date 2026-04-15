using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Data;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MVSystemApi.Interfaz;
using MVSystemApi.Model;
using MVSystemApi.Model_Negocio;
using DTO;
using DTO.FacEDGI;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace MVSystemApi.Controllers
{
    [Route("Factura")]
    [ApiController]
    public class FacturacionController : ControllerBase
    {
        private readonly Facturas_Negocio AD;
        private readonly IAccesoDatos _accesoDatos;
        private readonly IFacEDGIService _facEDGIService;
        private readonly PagoMixto_Negocio _pagoMixtoNegocio;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public FacturacionController(Facturas_Negocio _en, IAccesoDatos accesoDatos, IFacEDGIService facEDGIService, IServiceScopeFactory serviceScopeFactory)
        {
            _accesoDatos = accesoDatos;
            AD = _en;
            _facEDGIService = facEDGIService;
            _serviceScopeFactory = serviceScopeFactory;
            _pagoMixtoNegocio = new PagoMixto_Negocio();
        }
        [HttpPost]
        [Route("PostFactura")]
        //[Authorize(Roles = "MNU_MANT_FACTURACION")]
        public async Task<ActionResult> PostFactura([FromBody] FacturaRegistroDTO request)
        {
            try
            {
                // Validar pago mixto si aplica
                if (request.IdTipoPago == 6)
                {
                    if (request.DetallesPagos == null || request.DetallesPagos.Count < 2)
                        return BadRequest(new { error = "Pago Mixto requiere al menos 2 métodos de pago" });

                    if (!_pagoMixtoNegocio.ValidarPagoMixto(request.Total, request.DetallesPagos))
                        return BadRequest(new { error = "La suma de pagos no coincide con el total de la factura" });

                    if (!_pagoMixtoNegocio.ValidarMontosPositivos(request.DetallesPagos))
                        return BadRequest(new { error = "Todos los montos de pago deben ser positivos" });

                    if (!_pagoMixtoNegocio.ValidarSinTiposDuplicados(request.DetallesPagos))
                        return BadRequest(new { error = "No se puede seleccionar el mismo tipo de pago más de una vez" });
                }

                // Convertir DTO a modelo Facturas
                var factura = new Facturas
                {
                    NumeroFactura = request.NumeroFactura,
                    IdCliente = request.IdCliente,
                    IdVendedor = request.IdVendedor,
                    IdTipoFactura = request.IdTipoFactura,
                    IdTipoPago = request.IdTipoPago,
                    NcfTipoNumero = request.NcfTipoNumero,
                    FechaRegistro = request.FechaRegistro,
                    fechaLimitePago = request.FechaLimitePago,
                    SubTotal = request.SubTotal,
                    Descuento = request.Descuento,
                    Itbis = request.Itbis,
                    Total = request.Total,
                    Abono = (int)request.Abono,
                    RNC = request.RNC ?? "",
                    Nota = request.Nota ?? "",
                    Usuario = request.Usuario ?? "",
                    IdAlmacen = request.IdAlmacen,
                    empresa = request.Empresa ?? "",
                    IdEmpresa = request.IdEmpresa ?? "0",
                    IdSucursal = request.IdSucursal ?? "0",
                    CantidadArticulos = request.CantidadArticulos,
                    DetalleFacturaList = request.DetalleFacturaList?.Select(dto => new DetalleFactura
                    {
                        NumeroFactura = dto.NumeroFactura,
                        IdEquipo = dto.IdEquipo,
                        IdTipo = dto.IdTipo,
                        IdVendedor = request.IdVendedor,
                        IdTipoFactura = request.IdTipoFactura,
                        IdGarantia = dto.IdGarantia,
                        Cantidad = dto.Cantidad,
                        Precio = dto.Precio,
                        SubTotal = dto.SubTotal,
                        Descuento = dto.Descuento,
                        Itbis = dto.Itbis,
                        Total = dto.Total,
                        Usuario = dto.Usuario,
                        FechaRegistro = dto.FechaRegistro,
                        Descripcion = dto.Descripcion
                    }).ToList() ?? new List<DetalleFactura>()
                };

                // Preparar detalles de pago
                List<DetallePagoDTO> detallesPago;
                if (request.IdTipoPago == 6 && request.DetallesPagos?.Any() == true)
                {
                    detallesPago = request.DetallesPagos;
                }
                else
                {
                    detallesPago = new List<DetallePagoDTO>
                    {
                        new DetallePagoDTO
                        {
                            ID_Tipo_Pago = request.IdTipoPago,
                            Monto_Pago = request.Total,
                            Numero_Referencia = ""
                        }
                    };
                }

                // Guardar factura + artículos + pagos en una sola transacción
                var resultFactura = _accesoDatos.PostFacturaConPagos(factura, detallesPago);
                if (resultFactura == null)
                    return NotFound(new { error = "No se pudo guardar la factura" });

                // Asignar RNC de la empresa desde el token JWT
                var rncClaim = User?.FindFirst("Rnc")?.Value;
                if (!string.IsNullOrEmpty(rncClaim))
                    resultFactura.RncEmpresa = rncClaim;

                // Enviar factura a DGI de manera síncrona
                var errorDGI = await EnviarFacturaADGISincrono(resultFactura, factura.DetalleFacturaList, detallesPago);
                if (errorDGI != null)
                {
                    // DGI rechazó la factura: anularla
                    try
                    {
                        _accesoDatos.PostAnulaFactura(new FilterAnulaFactura
                        {
                            NumeroFactura = resultFactura.NumeroFactura,
                            IDsucursal = resultFactura.IdAlmacen?.ToString() ?? "0",
                            Usuario = request.Usuario ?? "Sistema"
                        });
                    }
                    catch (Exception exAnula)
                    {
                        Console.WriteLine($"[DGI-ANULA-ERROR] No se pudo anular factura {resultFactura.NumeroFactura}: {exAnula.Message}");
                    }

                    return BadRequest(new { error = "La DGII no pudo procesar la factura. Intente nuevamente.", errorDGI = true });
                }

                return Ok(new { factura = resultFactura, mensaje = "Factura guardada exitosamente" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet]
        [Route("GetDesglosePagos/{numeroFactura}")]
        //[Authorize(Roles = "MNU_CONS_FACTURAS")]
        public IActionResult GetDesglosePagos([FromRoute] int numeroFactura)
        {
            try
            {
                var result = _accesoDatos.GetDetallesPagoFactura(numeroFactura);
                if (result == null)
                    return NotFound(new { error = "No se encontraron detalles de pago" });

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        [HttpGet]
        [Route("GetFacturas")]
        //[Authorize(Roles = "MNU_CONS_FACTURAS")]
        public IActionResult GetFacturas([FromQuery] FacturaFilter consulta)
        {
            try
            {

                var result = AD.GetFacturas(consulta);
                if (result == null)return NotFound();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        [Route("GetDetalleFactura/{numeroFactura}/{sucursal}")]
        //[Authorize(Roles = "MNU_CONS_FACTURAS")]
        public IActionResult GetDetalleFactura([FromRoute] int numeroFactura, int sucursal)
        {
            try
            {
                var result = AD.GetDetalleFacturaConsulta(numeroFactura, sucursal);
                if (result == null)return NotFound();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet]
        [Route("GetFacturaNumeroUltimo")]
        //[Authorize(Roles = "MNU_CONS_FACTURAS")]
        public IActionResult GetFacturaNumeroUltimo()
        {
            try
            {
                var result = AD.GetFacturaNumeroUltimo();
                if (result == null) return NotFound();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        [Route("PostAnulaFactura")]
        //[Authorize(Roles = "MNU_MANT_FACTURACION")]
        public ActionResult<FilterAnulaFactura> PostAnulaFactura(FilterAnulaFactura Anulafactura)
        {
            try
            {
                var result = _accesoDatos.PostAnulaFactura(Anulafactura);
                if (result == null)
                {
                    return NotFound();
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        [Route("EditaDetalleFactura")]
        //[Authorize(Roles = "MNU_MANT_FACTURACION")]
        public ActionResult<EditaDetalleFactura> EditaDetalleFactura(EditaDetalleFactura EditaDetalleFactura)
        {
            try
            {
                var result = AD.EditaDetalleFactura(EditaDetalleFactura);
                if (result == null)
                {
                    return NotFound();
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost]
        [Route("DetalleFacturaRebajaEquipo")]
        //[Authorize(Roles = "MNU_MANT_FACTURACION")]
        public ActionResult<RebajaEquipoModel> DetalleFacturaRebajaEquipo(RebajaEquipoModel model)
        {
            try
            {
                var result = AD.RebajaEquipo(model);
                if (result == null)
                {
                    return NotFound();
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Envía factura a DGI de manera síncrona. Retorna null si fue exitoso, o el mensaje de error si falló.
        /// </summary>
        private async Task<string> EnviarFacturaADGISincrono(Facturas facturaGuardada, List<DetalleFactura> detalles, List<DetallePagoDTO> detallesPagoDTO)
        {
            // Verificar si la empresa está certificada para enviar a DGII
            var certificadoClaim = User?.FindFirst("Certificado")?.Value;
            Console.WriteLine($"[DGI-DEBUG] Factura {facturaGuardada.NumeroFactura} | Certificado={certificadoClaim} | NcfTipo={facturaGuardada.NcfTipoNumero}");
            if (!string.Equals(certificadoClaim, "True", StringComparison.OrdinalIgnoreCase))
                return null; // No es certificada, no enviar pero no es error

            // No enviar a DGII si el comprobante es "NO REQUERIDO" (NcfTipoNumero == 0)
            if (facturaGuardada.NcfTipoNumero == 0)
                return null;

            try
            {
                var usuarioDelToken = User?.Identity?.Name ?? "Sistema";
                var rncDelToken = User?.FindFirst("Rnc")?.Value ?? "";
                var empresaDelToken = User?.FindFirst("Empresa")?.Value ?? "";

                // Convertir pagos DTO a modelo
                var pagos = detallesPagoDTO?.Select(p => new DetallesPagos_Factura
                {
                    Numero_Factura = facturaGuardada.NumeroFactura,
                    ID_Tipo_Pago = p.ID_Tipo_Pago,
                    Monto_Pago = p.Monto_Pago,
                    Numero_Referencia = p.Numero_Referencia
                }).ToList() ?? new List<DetallesPagos_Factura>();

                // Obtener el NCF de la BD
                using var cn = new SqlConnection(_accesoDatos.ConnectionString);
                using var cmd = new SqlCommand("SP_Reporte_Factura", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Cod_Factura", facturaGuardada.NumeroFactura);
                cmd.Parameters.AddWithValue("@sucursal", facturaGuardada.IdAlmacen.Value);

                var da = new SqlDataAdapter(cmd);
                var dt = new DataTable();
                da.Fill(dt);

                if (dt != null && dt.Rows.Count > 0 && dt.Columns.Contains("NCF"))
                    facturaGuardada.NcfNumero = dt.Rows[0]["NCF"]?.ToString() ?? "";

                // Reintentar envío a DGI hasta 3 veces con espera entre intentos
                const int maxIntentos = 3;
                EasyConexionResponseDTO responseEDGI = null;
                for (int intento = 1; intento <= maxIntentos; intento++)
                {
                    responseEDGI = await _facEDGIService.EnviarFacturaADGII(facturaGuardada, detalles, pagos, usuarioDelToken, rncDelToken, empresaDelToken);

                    if (responseEDGI?.Success == true)
                        return null; // Éxito

                    Console.WriteLine($"[DGI-RETRY] Factura {facturaGuardada.NumeroFactura} | Intento {intento}/{maxIntentos} falló: {responseEDGI?.Message}");

                    if (intento < maxIntentos)
                        await Task.Delay(2000); // Esperar 2 segundos antes de reintentar
                }

                return responseEDGI?.Message ?? "Error desconocido al enviar a DGI";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DGI-ERROR] Factura {facturaGuardada.NumeroFactura}: {ex.Message}");
                return $"Error al comunicarse con DGI: {ex.Message}";
            }
        }
    }
}
