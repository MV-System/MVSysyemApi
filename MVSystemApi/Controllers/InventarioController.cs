using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MVSystemApi.Model;
using MVSystemApi.Model_Negocio;

namespace MVSystemApi.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class InventarioController : ControllerBase
    {
        private readonly Inventario_Negocio _AD;
        public InventarioController(Inventario_Negocio inventarioNegocio)
        {
            _AD = inventarioNegocio;
        }

        [HttpGet]
        [Route("{almacenId}")]
        [Authorize(Roles = "MNU_MANT_EQUIPOS")]
        public ActionResult<List<ArticuloInventario>> Get(int almacenId)
        {
            try
            {
                return Ok(_AD.ObtenerEquiposInventarioPorAlmacenId(almacenId));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("IniciarInventario")]
        [Authorize(Roles = "MNU_MANT_EQUIPOS")]
        public ActionResult IniciarInventario([FromBody] int almacenId)
        {
            try
            {
                _AD.IniciarInventario(almacenId);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpPost]
        [Route("CerrarInventario")]
        [Authorize(Roles = "MNU_MANT_EQUIPOS")]
        public ActionResult CerrarInventario([FromBody] int inventarioId)
        {
            try
            {
                _AD.CerrarInventario(inventarioId);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut]
        [Route("EscanearArticulo")]
        [Authorize(Roles = "MNU_MANT_EQUIPOS")]
        public ActionResult EscanearArticulo([FromBody] ArticuloInventarioUpdate data)
        {
            try
            {
                _AD.EscanearArticuloInventario(data);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
