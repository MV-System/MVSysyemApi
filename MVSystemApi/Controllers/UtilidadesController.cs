using Microsoft.AspNetCore.Mvc;
using MVSystemApi.Model;
using MVSystemApi.Model_Negocio;

namespace MVSystemApi.Controllers
{
    [Route("Utilidades")]
    [ApiController]
    public class UtilidadesController : ControllerBase
    {
        private readonly Utilidades_Negocio AD;
        public UtilidadesController(Utilidades_Negocio _ad)
        {
            AD = _ad;
        }

        [HttpGet()]
        [Route("GetUtilidades")]
        public ActionResult<Utilidades> Utilidades(int? sucursal, DateTime FI, DateTime FF)
        {

            try
            {
                var lista = AD.GetUtilidades(sucursal,FI,FF);
                if (lista == null)
                {
                    return NotFound();
                }
                return Ok(lista);
            }

            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet()]
        [Route("GetUtilidadesAsync")]
        public async Task<ActionResult<Utilidades>> UtilidadesAsync(int? sucursal, DateTime FI, DateTime FF)
        {
            try
            {
                var lista = await AD.GetUtilidadesAsync(sucursal, FI, FF);
                if (lista == null)
                {
                    return NotFound();
                }
                return Ok(lista);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("GetDetalleAbonos")]
        public ActionResult<List<DTO.UtilidadAbonoDetalleDTO>> GetDetalleAbonos(int? sucursal, DateTime FI, DateTime FF)
        {
            try
            {
                return Ok(AD.GetDetalleAbonos(sucursal, FI, FF));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
