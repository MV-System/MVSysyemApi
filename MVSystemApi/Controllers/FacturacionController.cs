using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MVSystemApi.Interfaz;
using MVSystemApi.Model;
using MVSystemApi.Model_Negocio;
using DTO;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace MVSystemApi.Controllers
{
    [Route("Factura")]
    [ApiController]
    public class FacturacionController : ControllerBase
    {
        private readonly Facturas_Negocio AD;
        private readonly IAccesoDatos _accesoDatos;
        private readonly PagoMixto_Negocio _pagoMixtoNegocio;
        public FacturacionController(Facturas_Negocio _en, IAccesoDatos accesoDatos)
        {
            _accesoDatos = accesoDatos;
            AD = _en;
            _pagoMixtoNegocio = new PagoMixto_Negocio();
        }
        [HttpPost]
        [Route("PostFactura")]
        //[Authorize(Roles = "MNU_MANT_FACTURACION")]
        public ActionResult<Facturas> PostFactura(Facturas factura)
        {
            try
            {
                var result = _accesoDatos.PostFactura(factura);
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
        [Route("PostFacturaPagoMixto")]
        //[Authorize(Roles = "MNU_MANT_FACTURACION")]
        public ActionResult PostFacturaPagoMixto([FromBody] FacturaRegistroDTO request)
        {
            try
            {
                Console.WriteLine($"🔵 BACKEND: Recibido PostFacturaPagoMixto - IdTipoPago: {request.IdTipoPago}");
                Console.WriteLine($"🔵 BACKEND: DetallesPagos count: {request.DetallesPagos?.Count ?? 0}");
                if (request.DetallesPagos?.Any() == true)
                {
                    foreach (var pago in request.DetallesPagos)
                    {
                        Console.WriteLine($"   - Tipo: {pago.ID_Tipo_Pago}, Monto: {pago.Monto_Pago}, Ref: {pago.Numero_Referencia}");
                    }
                }

                // Validar que si es pago mixto (idTipoPago == 6), tenga detalles
                if (request.IdTipoPago == 6) // 6 = Mixto
                {
                    if (request.DetallesPagos == null || request.DetallesPagos.Count == 0)
                        return BadRequest(new { error = "Pago Mixto requiere al menos 2 métodos de pago" });

                    if (request.DetallesPagos.Count < 2)
                        return BadRequest(new { error = "Pago Mixto debe tener mínimo 2 métodos de pago" });

                    // Validar que la suma de pagos = total factura
                    if (!_pagoMixtoNegocio.ValidarPagoMixto(request.Total, request.DetallesPagos))
                        return BadRequest(new { error = "La suma de pagos no coincide con el total de la factura" });

                    // Validar que los montos sean positivos
                    if (!_pagoMixtoNegocio.ValidarMontosPositivos(request.DetallesPagos))
                        return BadRequest(new { error = "Todos los montos de pago deben ser positivos" });
                }

                // Convertir DTO a modelo Facturas
                Console.WriteLine($"🔴 Convirtiendo DTO a modelo Facturas...");
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
                    // Mapear detalles de artículos del DTO al modelo
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
                Console.WriteLine($"✅ DTO convertido correctamente - Detalles: {factura.DetalleFacturaList?.Count ?? 0}");

                // Guardar factura
                Console.WriteLine($"💾 Guardando factura...");
                var resultFactura = _accesoDatos.PostFactura(factura);
                if (resultFactura == null)
                    return NotFound(new { error = "No se pudo guardar la factura" });
                Console.WriteLine($"✅ Factura guardada: {resultFactura.NumeroFactura}");

                // Si es pago MIXTO, guardar detalles
                if (request.IdTipoPago == 6 && request.DetallesPagos?.Any() == true)
                {
                    try
                    {
                        Console.WriteLine($"📝 Intentando guardar {request.DetallesPagos.Count} detalles de pago...");
                        _accesoDatos.PostDetallesPagoFactura(resultFactura.NumeroFactura, request.DetallesPagos);
                        Console.WriteLine($"✅ Detalles de pago guardados correctamente");
                    }
                    catch (Exception ex)
                    {
                        // Si falla al guardar detalles, aún retornamos éxito en la factura
                        // pero informamos del error
                        Console.WriteLine($"❌ Error al guardar detalles de pago: {ex.Message}");
                        Console.WriteLine($"   Stack: {ex.StackTrace}");
                        return Ok(new 
                        { 
                            factura = resultFactura,
                            advertencia = "Factura guardada pero hubo error al guardar detalles de pago: " + ex.Message 
                        });
                    }
                }

                return Ok(new { factura = resultFactura, mensaje = "Factura guardada exitosamente" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR GENERAL en PostFacturaPagoMixto: {ex.Message}");
                Console.WriteLine($"   Stack: {ex.StackTrace}");
                return BadRequest(new { error = ex.Message, stackTrace = ex.StackTrace });
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
    }
}
