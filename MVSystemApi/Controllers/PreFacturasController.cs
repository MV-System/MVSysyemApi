using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using MVSystemApi.Model;
using MVSystemApi.Model_Negocio;
using MVSystemApi.Interfaz;
using DTO;

namespace MVSystemApi.Controllers
{
    [Route("PreFacturas")]
    [ApiController]
    public class PreFacturasController : ControllerBase
    {
        private readonly PreFacturas_Negocio AD;
        private readonly IAccesoDatos _accesoDatos;
        private readonly IFacEDGIService _facEDGIService;

        public PreFacturasController(PreFacturas_Negocio _en, IAccesoDatos accesoDatos, IFacEDGIService facEDGIService)
        {
            AD = _en;
            _accesoDatos = accesoDatos;
            _facEDGIService = facEDGIService;
        }

        // POST /PreFacturas
        [HttpPost]
        [Route("")]
        public ActionResult<PreFactura> CrearPreFactura([FromBody] PreFactura prefactura)
        {
            try
            {
                var result = AD.CrearPreFactura(prefactura);
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

        // POST /PreFacturas/detalle
        [HttpPost]
        [Route("detalle")]
        public ActionResult<PreDetalleFactura> AgregarDetalle([FromBody] PreDetalleFactura detalle)
        {
            try
            {
                var result = AD.AgregarDetallePreFactura(detalle);
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

        // GET /PreFacturas
        [HttpGet]
        [Route("")]
        public IActionResult ListarPreFacturas([FromQuery] PreFacturaFiltro filtro)
        {
            try
            {
                var result = AD.ListarPreFacturas(filtro);
                if (result == null) return NotFound();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // GET /PreFacturas/{id}/detalle
        [HttpGet]
        [Route("{id}/detalle")]
        public IActionResult ObtenerDetalle(int id)
        {
            try
            {
                var result = AD.ObtenerDetallePreFactura(id);
                if (result == null) return NotFound();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        public class ProcesarPreFacturaRequest
        {
            public string UsuarioProcesamiento { get; set; }
        }

        // POST /PreFacturas/{id}/procesar
        [HttpPost]
        [Route("{id}/procesar")]
        public IActionResult ProcesarPreFactura(int id, [FromBody] ProcesarPreFacturaRequest request)
        {
            try
            {
                // Obtener datos de la pre-factura ANTES de procesar (para envío a DGI)
                var preFacturaData = AD.ObtenerPreFacturaPorId(id);
                var preDetalles = AD.ObtenerDetallePreFactura(id);

                var result = AD.ProcesarPreFactura(id, request.UsuarioProcesamiento);
                if (result == null) return NotFound();

                // Enviar a DGI si corresponde
                var codFacturaProperty = result.GetType().GetProperty("Cod_Factura");
                if (codFacturaProperty != null && preFacturaData != null)
                {
                    int nuevoNumeroFactura = Convert.ToInt32(codFacturaProperty.GetValue(result));
                    if (nuevoNumeroFactura > 0)
                    {
                        EnviarFacturaADGIEnBackground(preFacturaData, preDetalles, nuevoNumeroFactura);
                    }
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // POST /PreFacturas/{id}/anular
        [HttpPost]
        [Route("{id}/anular")]
        public IActionResult AnularPreFactura(int id)
        {
            try
            {
                var result = AD.AnularPreFactura(id);
                if (result == null) return NotFound();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        public class RebajarItemPreFacturaRequest
        {
            public int NumeroPreFactura { get; set; }
            public int IdEquipo { get; set; }
        }

        // POST /PreFacturas/rebajar-item
        [HttpPost]
        [Route("rebajar-item")]
        public IActionResult RebajarItemPreFactura([FromBody] RebajarItemPreFacturaRequest request)
        {
            try
            {
                var result = AD.RebajarItemPreFactura(request.NumeroPreFactura, request.IdEquipo);
                if (result == null) return NotFound();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // POST /PreFacturas/{id}/detalles-pago (guardar detalles de pago mixto)
        [HttpPost]
        [Route("{id}/detalles-pago")]
        public IActionResult GuardarDetallesPagoPreFactura(int id, [FromBody] List<DetallePagoDTO> detallesPagos)
        {
            try
            {
                if (detallesPagos == null || detallesPagos.Count == 0)
                    return BadRequest(new { error = "Debe proporcionar detalles de pago" });

                _accesoDatos.PostDetallesPagoFactura(id, detallesPagos);
                return Ok(new { mensaje = "Detalles de pago guardados correctamente" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Envía la factura procesada a DGI en background si la empresa está certificada y tiene comprobante
        /// </summary>
        private void EnviarFacturaADGIEnBackground(PreFactura preFactura, List<PreDetalleFactura> preDetalles, int numeroFactura)
        {
            // Verificar si la empresa está certificada
            var certificadoClaim = User?.FindFirst("Certificado")?.Value;
            if (!string.Equals(certificadoClaim, "True", StringComparison.OrdinalIgnoreCase))
                return;

            // No enviar si comprobante es "NO REQUERIDO"
            int ncfTipo = 0;
            int.TryParse(preFactura.NFCTipoNumero, out ncfTipo);
            if (ncfTipo == 0)
                return;

            // Capturar datos del contexto antes del Task.Run
            var usuarioDelToken = User?.Identity?.Name ?? "Sistema";
            var rncDelToken = User?.FindFirst("Rnc")?.Value ?? "";
            var empresaDelToken = User?.FindFirst("Empresa")?.Value ?? "";
            var connectionString = _accesoDatos.ConnectionString;
            var sucursal = preFactura.ID_Sucursal;

            // Construir objeto Facturas desde la pre-factura
            var facturaParaDGI = new Facturas
            {
                NumeroFactura = numeroFactura,
                IdCliente = preFactura.ID_Cliente,
                IdVendedor = preFactura.ID_Vendedor ?? 0,
                IdTipoFactura = preFactura.ID_Tipo_Factura,
                IdTipoPago = preFactura.ID_Tipo_Pagos ?? 0,
                NcfTipoNumero = ncfTipo,
                SubTotal = preFactura.SubTotal,
                Descuento = preFactura.Descuento ?? 0,
                Itbis = preFactura.Itbis ?? 0,
                Total = preFactura.Total,
                Abono = (int)(preFactura.Abono ?? 0),
                RNC = preFactura.RNC ?? "",
                Nota = preFactura.Nota ?? "",
                IdAlmacen = sucursal,
                RncEmpresa = rncDelToken
            };

            // Convertir detalles
            var detalles = preDetalles?.Select(d => new DetalleFactura
            {
                NumeroFactura = numeroFactura,
                IdEquipo = d.ID_Equipo,
                IdTipo = d.ID_Tipo,
                IdGarantia = d.ID_Garantia,
                Cantidad = d.cantidad ?? 0,
                Precio = d.precio ?? 0,
                SubTotal = d.SubTotal,
                Descuento = d.Descuento ?? 0,
                Itbis = d.Itbis ?? 0,
                Total = d.Total,
                Descripcion = d.Descripcion
            }).ToList() ?? new List<DetalleFactura>();

            // Obtener pagos
            var pagosDTO = _accesoDatos.GetDetallesPagoFactura(numeroFactura);
            var pagosCapturados = pagosDTO?.Select(p => new DetallesPagos_Factura
            {
                Numero_Factura = numeroFactura,
                ID_Tipo_Pago = p.ID_Tipo_Pago,
                Monto_Pago = p.Monto_Pago,
                Numero_Referencia = p.Numero_Referencia
            }).ToList() ?? new List<DetallesPagos_Factura>();

            _ = Task.Run(async () =>
            {
                try
                {
                    // Obtener NCF asignado
                    using var cn = new SqlConnection(connectionString);
                    using var cmd = new SqlCommand("SP_Reporte_Factura", cn);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@Cod_Factura", numeroFactura);
                    cmd.Parameters.AddWithValue("@sucursal", sucursal);

                    var da = new SqlDataAdapter(cmd);
                    var dt = new DataTable();
                    da.Fill(dt);

                    if (dt != null && dt.Rows.Count > 0 && dt.Columns.Contains("NCF"))
                    {
                        facturaParaDGI.NcfNumero = dt.Rows[0]["NCF"]?.ToString() ?? "";
                    }

                    await _facEDGIService.EnviarFacturaADGII(facturaParaDGI, detalles, pagosCapturados, usuarioDelToken, rncDelToken, empresaDelToken);
                }
                catch (Exception)
                {
                }
            });
        }
    }
}
