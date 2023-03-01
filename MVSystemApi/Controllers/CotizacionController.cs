using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MVSystemApi.Interfaz;
using MVSystemApi.Model;
using MVSystemApi.Model_Negocio;


namespace MVSystemApi.Controllers
{
    [Route("Cotizacion")]
    [ApiController]
    public class CotizacionController : Controller
    {
        private readonly IAccesoDatos _accesoDatos;
        public CotizacionController(IAccesoDatos accesoDatos)
        {
            _accesoDatos = accesoDatos;
        }

        [HttpGet("GetCotizacionFacturaConsulta/{telefono}")]
        //[Authorize(Roles = "MNU_MANT_FACTURACION")]
        public IActionResult GetCotizacionFacturaConsulta([FromRoute] string telefono)
        {
            try
            {
                var result = _accesoDatos.GetCotizacionFacturaConsulta(telefono);
                if (result == null) return NotFound();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet("GetDetalleCotizacionConsulta/{numeroFactura}/{sucursal}")]
        //[Authorize(Roles = "MNU_MANT_FACTURACION")]
        public IActionResult GetDetalleCotizacionConsulta([FromRoute] int numeroFactura, int sucursal)
        {
            try
            {
                var result = _accesoDatos.GetDetalleCotizacionConsulta(numeroFactura,sucursal);
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
