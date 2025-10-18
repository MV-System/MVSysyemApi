using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using MVSystemApi.Model;
using MVSystemApi.Model_Negocio;

namespace MVSystemApi.Controllers
{
    [Route("PreFacturas")]
    [ApiController]
    public class PreFacturasController : ControllerBase
    {
        private readonly PreFacturas_Negocio AD;
        public PreFacturasController(PreFacturas_Negocio _en)
        {
            AD = _en;
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
    }
}
