using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MVSystemApi.Model;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace MVSystemApi.Controllers
{
    [Route("Clientes")]
    [ApiController]
    public class ClientesController : ControllerBase
    {
        private readonly Clientes_Negocio AD;


        public ClientesController(Clientes_Negocio _cn)
        {
            AD = _cn;
        }
        // GET: /<controller>/
        [HttpPost()]
        [Route("Cliente_Insert")]
        [Authorize("MNU_MANT_CLIENTES")]
        public ActionResult<Clientes> Cliente_Insert([FromBody]Clientes cliente)
        {

            try
            {
                var lista = AD.Cliente_Insert(cliente);
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
        [Route("Get_Cliente")]
        [Authorize("MNU_CONS_CLIENTES")]
        public ActionResult<Clientes> Get_Cliente(string criterio)
        {

            try
            {
                var lista = AD.Cliente_Consulta(criterio);
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
