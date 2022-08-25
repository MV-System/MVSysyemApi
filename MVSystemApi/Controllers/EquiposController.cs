using System;
using DTO;
using Microsoft.AspNetCore.Mvc;
using MVSystemApi.Interfaz;
using MVSystemApi.Model;
using Newtonsoft.Json;

namespace MVSystemApi.Controllers
{
    [Route("Equipos")]
    [ApiController]
    public class EquiposController : ControllerBase
    {
        private readonly Equipos_Negocio AD;
        private readonly IAccesoDatos _accesoDatos;
        public EquiposController(Equipos_Negocio _en, IAccesoDatos accesoDatos)
        {
            _accesoDatos = accesoDatos;
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
        [Route("GetEquiposDisponible")]
        public ActionResult<EquipoDisponibleDTO> GetEquiposDisponible([FromQuery] EquipoDisponibleQueryDTO? equipoDisponibleQueryDTO)
        {
            try
            {

                var result = AD.GetEquiposDisponible(equipoDisponibleQueryDTO);




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
