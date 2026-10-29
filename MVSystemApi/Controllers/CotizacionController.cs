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
        private readonly Cotizacion_Negocio _cotizacionNegocio;
        public CotizacionController(IAccesoDatos accesoDatos, Cotizacion_Negocio cotizacionNegocio)
        {
            _accesoDatos = accesoDatos;
            _cotizacionNegocio = cotizacionNegocio;
        }

        [HttpGet("Consulta")]
        public IActionResult GetCotizaciones([FromQuery] CotizacionConsultaFiltro filtro)
        {
            try
            {
                var result = _cotizacionNegocio.GetCotizaciones(filtro);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Cancela una cotización no facturada (se oculta de la lista, no se borra).
        /// PUT: Cotizacion/Cancelar
        /// </summary>
        [HttpPut("Cancelar")]
        public IActionResult CancelarCotizacion([FromBody] CotizacionCancelar datos)
        {
            try
            {
                if (datos == null || datos.Codigo <= 0)
                {
                    return BadRequest("La cotización es requerida.");
                }
                var result = _accesoDatos.Cotizacion_Cancelar(datos);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("Detalle/{codigo}")]
        public IActionResult GetDetalleCotizacion(int codigo)
        {
            try
            {
                var cotizacion = _cotizacionNegocio.GetCotizacionPorCodigo(codigo);
                if (cotizacion == null) return NotFound();
                return Ok(cotizacion.Articulos);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
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
