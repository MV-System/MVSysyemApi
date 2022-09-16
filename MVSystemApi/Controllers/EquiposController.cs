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
        [Route("GetEquiposVendidos")]
        public ActionResult<EquipoVendido> GetEquiposVendidos([FromQuery] EquipoVendidoFilter? equipoVendidoFilter)
        {
            try
            {
                var result = AD.GetEquiposVendidos(equipoVendidoFilter);

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
        public ActionResult<EquipoDisponibleDTO> GetEquiposDisponible([FromQuery] EquipoDisponibleFilterDTO? equipoDisponibleFilterDTO)
        {
            try
            {
                var result = AD.GetEquiposDisponible(equipoDisponibleFilterDTO);

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
        [HttpGet]
        [Route("GetEquipoByImei")]
        public ActionResult<EquipoImei> GetEquipoByImei(string imei)
        {
            try
            {

                var result = AD.GetEquipoByImei(imei);
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
        [Route("GetEquipoUltimoIdTransferencia")]
        public ActionResult<int> GetEquipoUltimoIdTransferencia()
        {
            try
            {

                var result = AD.GetEquipoUltimoIdTransferencia();
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
        [HttpPost]
        [Route("PostEquipoTransferencia")]
        public ActionResult<EquipoTransferencia> PostEquipoTransferencia([FromBody] EquipoTransferencia tranferencia)
        {
            try
            {

                var result = _accesoDatos.PostEquipoTransferencia(tranferencia);
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
