using System;
using DTO;
using Microsoft.AspNetCore.Authorization;
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
        //[Authorize(Roles = "MNU_MANT_EQUIPOS")]
        public ActionResult<Equipos> Equipo_Insert([FromBody] Equipos Equipo)
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

        [HttpGet("Equipo_Busca_Disponible")]
        //[Authorize(Roles = "MNU_CONS_EQUIPOS")]
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
       //[Authorize(Roles = "equiposVendidosMenu")]
        public ActionResult<List<EquipoVendido>> GetEquiposVendidos([FromQuery] EquipoVendidoFilter equipoVendidoFilter)
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
       //[Authorize(Roles = "MNU_CONS_EQUIPOS")]
        public ActionResult<List<EquipoDisponibleDTO>> GetEquiposDisponible([FromQuery] EquipoDisponibleFilterDTO equipoDisponibleFilterDTO)
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
        [Route("GetEquipoByImei")]
        public ActionResult<EquipoTransferencia> GetEquipoByImei([FromQuery] string imei)
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
        [Route("GetEquipoPreciosEstatusByImei")]
        //[Authorize(Roles = "cambiarEstadoDeEquipoToolStripMenuItem")]
        public ActionResult<Equipo_return_Imei> GetEquipoPreciosEstatusByImei([FromQuery] string imei)
        {
            try
            {
                var result = AD.GetEquipoPreciosEstatusByImei(imei);

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

        [HttpPut]
        [Route("ModificarEstatusEquipo")]
        //[Authorize(Roles = "cambiarEstadoDeEquipoToolStripMenuItem")]
        public ActionResult ModificarEquipo([FromBody] Equipo_return_Imei equipo)
        {
            try
            {
                AD.ModificarEquipo(equipo);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("RecepcionEquipos")]
        //[Authorize(Roles = "MNU_MANT_EQUIPOS")]
        public ActionResult RecibirEquipo(EquipoRecepcion equipo)
        {
            try
            {
                AD.InsertarEquipoRecibido(equipo);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet]
        [Route("GetRecepcionEquipos")]
      // [Authorize(Roles = "MNU_MANT_EQUIPOS")]
        public ActionResult<List<EquipoRecepcionGet>> GetEquiposRecibidos([FromQuery]CriterioFilters filterData)
        {
            try
            {
                var result = AD.GetEquiposRecibidos(filterData);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost]
        [Route("EstadoActualiza")]
        //[Authorize(Roles = "MNU_MANT_EQUIPOS")]
        public ActionResult EquipoEstadoActualiza(EquipoEstadoUpdate dataActualizar)
        {
            try
            {
                AD.EquipoEstadoActualiza(dataActualizar);
                return Ok();
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
       // [Authorize(Roles = "MNTransfiereEquipos")]
        public ActionResult<EquipoTransferencia> PostEquipoTransferencia([FromBody] EquipoTransferencia tranferencia)
        {
            try
            {
                var result = AD.PostEquipoTransferencia(tranferencia);
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
        [Route("GetTransferenciaEquipos")]
        public ActionResult<FiltroTransferencias> GetTransferenciaEquipos([FromQuery] FiltroTransferencias param)
        {
            try
            {

                var result = AD.GetTransferenciaEquipos(param.Sucursal_Salida,param.Sucursal_Destino,param.FechaInicio,param.FechaFinal);
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
        [Route("GetDetalleTransferenciaEquipos")]
        public ActionResult<FiltroTransferencias> GetDetalleTransferenciaEquipos([FromQuery] int IdTransferencia)
        {
            try
            {

                var result = AD.GetDetalleTransferenciaEquipos(IdTransferencia);
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
        [Route("PostEquipo_Delete")]
        // [Authorize(Roles = "MNTransfiereEquipos")]
        public ActionResult<EquipoTransferencia> PostEliminaEquipodeFactura([FromQuery] string imei, string Usuario)
        {
            try
            {
                var result = AD.Equipo_Delete(imei, Usuario);
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
        [Route("PostReparacionEquipo")]
        // [Authorize(Roles = "MNTransfiereEquipos")]
        public ActionResult<ReparacionesInsert> PostReparacionEquipo([FromBody] ReparacionesInsert Reparaciones)
        {
            try
            {
                var result = AD.EquiposReparacionInsert(Reparaciones);
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
[Route("GetReparaciones")]
public ActionResult<List<ReparacionReporteDTO>> GetReparaciones([FromQuery] ReparacionReporteFiltro filtro)
        {
            try
            {
                var result = _accesoDatos.ObtenerReparaciones(filtro);
                if (result == null || !result.Any())
                {
                    return Ok(new { message = "No se encontraron reparaciones.", data = new List<ReparacionReporteDTO>() });
                }
                return Ok(new { message = "Consulta exitosa.", data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
