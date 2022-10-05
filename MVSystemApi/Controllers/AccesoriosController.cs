using Microsoft.AspNetCore.Mvc;
using MVSystemApi.Model;
using Newtonsoft.Json;

namespace MVSystemApi.Controllers
{
    [Route("Accesorios")]
    [ApiController]
    public class AccesoriosController : ControllerBase
    {
        private readonly Accesorios_Negocio AD;

        public AccesoriosController(Accesorios_Negocio _ad)
        {
            AD = _ad;
        }
        [HttpPost]
        [Route("Accesorio_Insert")]
        public ActionResult<Accesorio> Accesorio_Insert([FromBody] Accesorio accesorio)
        {

            try
            {
                var lista = AD.Accesorio_Insert(accesorio);
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
        [Route("GetAccesorio")]
        public ActionResult<Accesorio> GetAccesorio(int codigo)
        {

            try
            {
                var lista = AD.ConsultaAccesorio(codigo);
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

        [HttpGet("GetAllAccesorios")]
        public ActionResult<List<Accesorio>> GetAllAccesorios([FromQuery] Paginate paginate, string accesorio, int almacen)
        {

            try
            {
                var lista = AD.GetAllAccesorios(paginate,accesorio,almacen);

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
    }
}
