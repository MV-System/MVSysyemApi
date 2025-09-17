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
        //[Authorize(Roles = "MNU_MANT_CLIENTES")]
        public ActionResult<Clientes> Cliente_Insert([FromBody] Clientes cliente)
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

        [HttpPut()]
        [Route("Cliente_Update")]
        //[Authorize(Roles = "MNU_MANT_CLIENTES")]
        public ActionResult<Clientes> Cliente_Update([FromBody] Clientes cliente)
        {
            try
            {
                var resultado = AD.Cliente_Update(cliente);
                if (resultado == null)
                {
                    return NotFound();
                }
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet()]
        [Route("Get_Cliente")]
        //[Authorize(Roles = "MNU_CONS_CLIENTES")]
        public ActionResult<List<Clientes_consulta>> Get_Cliente(string criterio)
        {

            try
            {
                var lista = AD.Cliente_Consulta(criterio) as List<Clientes_consulta>;
                if (lista == null || lista.Count == 0)
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
        [Route("GetClientesConFacturasPendientes")]
        //[Authorize(Roles = "MNU_CONS_CLIENTES")]
        public ActionResult<ObtenerClientesConFacturasPendientes> GetClientesConFacturasPendientes([FromQuery] string FechaInicio, string FechaFin, string Criterio)
        {

            try
            {
                var lista = AD.Cliente_FacturasPendientes(FechaInicio,FechaFin,Criterio);
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
        [Route("GetFacturasPendientesPorCliente")]
        //[Authorize(Roles = "MNU_CONS_CLIENTES")]
        public ActionResult<ObtenerClientesConFacturasPendientes> GetFacturasPendientesPorCliente([FromQuery] int cliente)
        {

            try
            {
                var lista = AD.Cliente_FacturasPendientesDetalle(cliente);
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
        [HttpPost()]
        [Route("AbonoAfactura")]
        //[Authorize(Roles = "MNU_MANT_CLIENTES")]
        public ActionResult<string> AbonoAfactura([FromBody] AbonoFacturaParam param)
        {

            try
            {
                var result = AD.AbonoAfactura(param);
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
        [HttpPost()]
        [Route("AbonoGeneral")]
        //[Authorize(Roles = "MNU_MANT_CLIENTES")]
        public ActionResult<string> AbonoGeneral([FromBody] AbonoFacturaParam param)
        {

            try
            {
                var result = AD.AbonoGeneral(param);
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
    }
}
