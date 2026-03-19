using System;
using System.Collections.Generic;
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
        
        public PreFacturasController(PreFacturas_Negocio _en, IAccesoDatos accesoDatos)
        {
            AD = _en;
            _accesoDatos = accesoDatos;
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
                var result = AD.ProcesarPreFactura(id, request.UsuarioProcesamiento);
                if (result == null) return NotFound();
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
                Console.WriteLine($"🔵 BACKEND PREFACTURA: Recibido GuardarDetallesPagoPreFactura para PreFactura #{id}");
                Console.WriteLine($"   Cantidad de detalles recibidos: {detallesPagos?.Count ?? 0}");
                
                if (detallesPagos == null || detallesPagos.Count == 0)
                {
                    Console.WriteLine($"❌ BACKEND PREFACTURA: Detalles de pago nulos o vacíos");
                    return BadRequest(new { error = "Debe proporcionar detalles de pago" });
                }

                // Log de cada detalle
                foreach (var pago in detallesPagos)
                {
                    Console.WriteLine($"   - TipoPago: {pago.ID_Tipo_Pago}, Monto: {pago.Monto_Pago}, Ref: {pago.Numero_Referencia}");
                }

                Console.WriteLine($"💾 BACKEND PREFACTURA: Guardando detalles de pago...");
                _accesoDatos.PostDetallesPagoFactura(id, detallesPagos);
                
                Console.WriteLine($"✅ BACKEND PREFACTURA: Detalles de pago guardados correctamente para PreFactura #{id}");
                return Ok(new { mensaje = "Detalles de pago guardados correctamente" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR BACKEND PREFACTURA: {ex.Message}");
                Console.WriteLine($"   Stack: {ex.StackTrace}");
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
