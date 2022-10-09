using DTO;
using Microsoft.AspNetCore.Mvc;
using MVSystemApi.Interfaz;
using MVSystemApi.Model;
using Newtonsoft.Json;

namespace MVSystemApi.Controllers
{
    [Route("Accesorios")]
    [ApiController]
    public class AccesoriosController : ControllerBase
    {
        private readonly Accesorios_Negocio AD;
        private readonly IAccesoDatos accesoDatos;

        public AccesoriosController(Accesorios_Negocio _ad,IAccesoDatos accesoDatos)
        {
            AD = _ad;
            this.accesoDatos = accesoDatos;
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
        [HttpPut]
        [Route("PutAccesorio")]
        public ActionResult<Accesorio> UpdateAccesorio([FromBody] AccesorioDTO accesorio)
        {

            try
            {
                var lista = accesoDatos.UpdateAccesorio(accesorio);
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
        [HttpGet()]
        [Route("GetAccesorioById{id}")]
        public ActionResult<Accesorio> GetAccesorioById([FromRoute]int id)
        {

            try
            {
                var lista = AD.GetAccesorioById(id);
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
        [HttpGet("GetAccesorios")]
        public ActionResult<List<Accesorio>> GetAccesorios([FromQuery] Paginate paginate, string accesorio, int almacen)
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
        [HttpGet("GetAccesoriosVendidos")]
        public ActionResult<List<Accesorio>> GetAccesoriosVendidos([FromQuery] Paginate paginate, string accesorio, int almacen)
        {

            try
            {
                var lista = AD.GetAccesoriosVendidos(paginate, accesorio, almacen);

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
