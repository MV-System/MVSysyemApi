using System;
using Microsoft.AspNetCore.Mvc;
using MVSystemApi.Model;

namespace MVSystemApi.Controllers
{
    [Route("Equipos")]
    [ApiController]
    public class EquiposController : ControllerBase
    {
        private readonly Equipos_Negocio AD;
        public EquiposController(Equipos_Negocio _en)
        {
            AD = _en;
        }

        [HttpPost]
        [Route("Equipo_Insert")]
        public ActionResult<Equipos> Equipo_Insert([FromBody]Equipos Equipo)
        {
            try
            {

                var result = AD.Equipo_Insert(Equipo);
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

        [HttpGet]
        [Route("Equipo_Busca_Disponible")]
        public ActionResult<Equipos> Equipo_Busca_Disponible(string Equipo, int Id_Almacen)
        {
            try
            {
                var result = AD.Equipo_Busca_Disponible(Equipo, Id_Almacen);
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

        [HttpGet]
        [Route("Numero_Registro")]
        public ActionResult<Equipos> Numero_Registro()
        {
            try
            {

                var result = AD.Numero_Registro();
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
