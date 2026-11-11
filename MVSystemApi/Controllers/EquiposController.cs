using System;
using MVSystemApi.Handlers;
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
        [RequierePermiso(PermisosApi.MantEquipos)]
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
                return BadRequest(ErroresApi.Mensaje(ex));
            }
        }

        [HttpGet("Equipo_Busca_Disponible")]
        [RequierePermiso(PermisosApi.ConsEquipos, PermisosApi.MantFacturacion, PermisosApi.ConsFacturas, PermisosApi.CotizacionMenu)]
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
                return BadRequest(ErroresApi.Mensaje(ex));
            }
        }
        [HttpGet]
        [Route("GetEquiposVendidos")]
        [RequierePermiso(PermisosApi.EquiposVendidos, PermisosApi.VerUtilidades)]
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
                return BadRequest(ErroresApi.Mensaje(ex));
            }
        }
        [HttpGet]
        [Route("GetEquiposDisponible")]
        [RequierePermiso(PermisosApi.ConsEquipos, PermisosApi.VerUtilidades)]
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
                return BadRequest(ErroresApi.Mensaje(ex));
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
                return BadRequest(ErroresApi.Mensaje(ex));
            }
        }

        [HttpGet]
        [Route("GetEquipoPreciosEstatusByImei")]
        [RequierePermiso(PermisosApi.CambiarEstadoEquipo, PermisosApi.TransfiereEquipos)]
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
                return BadRequest(ErroresApi.Mensaje(ex));
            }
        }

        [HttpPut]
        [Route("ModificarEstatusEquipo")]
        [RequierePermiso(PermisosApi.CambiarEstadoEquipo, PermisosApi.TransfiereEquipos)]
        public ActionResult ModificarEquipo([FromBody] Equipo_return_Imei equipo)
        {
            try
            {
                AD.ModificarEquipo(equipo);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ErroresApi.Mensaje(ex));
            }
        }

        [HttpPost]
        [Route("RecepcionEquipos")]
        [RequierePermiso(PermisosApi.MantEquipos, PermisosApi.RecepcionEquiposMenu, PermisosApi.RecepcionEquipos, PermisosApi.ConsRecepcionEquipos)]
        public ActionResult RecibirEquipo(EquipoRecepcion equipo)
        {
            try
            {
                AD.InsertarEquipoRecibido(equipo);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ErroresApi.Mensaje(ex));
            }
        }

        [HttpGet]
        [Route("GetRecepcionEquipos")]
        [RequierePermiso(PermisosApi.MantEquipos, PermisosApi.RecepcionEquiposMenu, PermisosApi.RecepcionEquipos, PermisosApi.ConsRecepcionEquipos)]
        public ActionResult<List<EquipoRecepcionGet>> GetEquiposRecibidos([FromQuery]CriterioFilters filterData)
        {
            try
            {
                var result = AD.GetEquiposRecibidos(filterData);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ErroresApi.Mensaje(ex));
            }
        }

        [HttpPost]
        [Route("EstadoActualiza")]
        [RequierePermiso(PermisosApi.MantEquipos, PermisosApi.RecepcionEquiposMenu, PermisosApi.RecepcionEquipos, PermisosApi.ConsRecepcionEquipos)]
        public ActionResult EquipoEstadoActualiza(EquipoEstadoUpdate dataActualizar)
        {
            try
            {
                AD.EquipoEstadoActualiza(dataActualizar);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ErroresApi.Mensaje(ex));
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
                return BadRequest(ErroresApi.Mensaje(ex));
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
                return BadRequest(ErroresApi.Mensaje(ex));
            }
        }
        [HttpPost]
        [Route("PostEquipoTransferencia")]
        [RequierePermiso(PermisosApi.TransfiereEquipos)]
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
                return BadRequest(ErroresApi.Mensaje(ex));
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
                return BadRequest(ErroresApi.Mensaje(ex));
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
                return BadRequest(ErroresApi.Mensaje(ex));
            }
        }
        [HttpGet]
        [Route("ValidarEliminacionEquipo")]
        [RequierePermiso(PermisosApi.GestionarEquipo)]   // solo la pantalla Gestionar equipo lo usa
        public ActionResult<EquipoValidacionEliminacion> ValidarEliminacionEquipo([FromQuery] string imei)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(imei))
                {
                    return BadRequest("Debe indicar un IMEI.");
                }

                var result = AD.ValidarEliminacionEquipo(imei);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ErroresApi.Mensaje(ex));
            }
        }

        [Authorize]
        [HttpGet]
        [Route("GestionarEquipo/ConexionActual")]
        public ActionResult<ConsultaSqlConexion> ObtenerConexionActualGestionarEquipo()
        {
            if (!TienePermisoModoDesarrollador())
            {
                return Forbid();
            }

            try
            {
                return Ok(AD.ObtenerConexionActualGestionarEquipo());
            }
            catch (Exception ex)
            {
                return BadRequest($"No se pudo obtener la conexión SQL actual: {ex.GetBaseException().Message}");
            }
        }

        [Authorize]
        [HttpPost]
        [Route("GestionarEquipo/ConsultaSql")]
        public ActionResult<ConsultaSqlResultado> EjecutarConsultaSqlGestionarEquipo([FromBody] ConsultaSqlRequest request)
        {
            try
            {
                if (!TienePermisoModoDesarrollador())
                {
                    return Forbid();
                }

                if (request == null || string.IsNullOrWhiteSpace(request.Consulta))
                {
                    return BadRequest("Escribe una consulta SELECT, UPDATE o DELETE para ejecutar.");
                }

                return Ok(AD.EjecutarConsultaSql(request.Consulta, 200));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ErroresApi.Mensaje(ex));
            }
            catch (Exception ex)
            {
                return BadRequest($"Error SQL: {ex.GetBaseException().Message}");
            }
        }

        private bool TienePermisoModoDesarrollador()
        {
            return TienePermiso("ModoDesarrollador");
        }

        private bool TienePermiso(string permiso)
        {
            return User.Claims.Any(claim =>
                (claim.Type == "role" || claim.Type == System.Security.Claims.ClaimTypes.Role) &&
                claim.Value == permiso);
        }

        [HttpPost]
        [Route("PostEquipo_Delete")]
        [RequierePermiso(PermisosApi.MantEquipos)]
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
                return BadRequest(ErroresApi.Mensaje(ex));
            }
        }

        [HttpPost]
        [Route("GestionarEquipo_EliminarRegistro")]
        [RequierePermiso(PermisosApi.GestionarEquipo)]   // borra registros del equipo: mismo permiso que la pantalla Gestionar equipo
        public ActionResult<EquipoEliminado> EliminarRegistroEquipoDeTabla([FromQuery] string imei, [FromQuery] string tabla)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(imei) || string.IsNullOrWhiteSpace(tabla))
                {
                    return BadRequest("Debe indicar el IMEI y la tabla que desea eliminar.");
                }

                var usuario = User?.Identity?.Name ?? string.Empty;
                return Ok(AD.EliminarRegistroEquipoDeTabla(imei, tabla, usuario));
            }
            catch (Exception ex)
            {
                return BadRequest(ErroresApi.Mensaje(ex));
            }
        }
        [HttpPost]
        [Route("PostReparacionEquipo")]
        [RequierePermiso(PermisosApi.MantReparacion)]
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
                return BadRequest(ErroresApi.Mensaje(ex));
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
                return BadRequest(ErroresApi.Mensaje(ex));
            }
        }
    
    [HttpGet]
        [Route("GetReparaciones_Lista")]
        public ActionResult<ReparacionListaDTO> GetReparaciones_Lista([FromQuery] ReparacionConsultaFiltro filtro)
        {
            try
            {
                var result = _accesoDatos.GetReparacionesLista(filtro);
                if (result == null)
                {
                    return Ok(new { message = "No se encontraron reparaciones.", data = new List<ReparacionReporteDTO>() });
                }
                return Ok(new { message = "Consulta exitosa.", data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(ErroresApi.Mensaje(ex));
            }
        }
        /// <summary>
        /// Cancela una reparación abierta (Pendiente / En Proceso). Requiere el permiso REPARACION_CANCELAR.
        /// PUT: Equipos/CancelarReparacion
        /// </summary>
        [HttpPut]
        [Route("CancelarReparacion")]
        public ActionResult CancelarReparacion([FromBody] ReparacionCancelar datos)
        {
            if (!TienePermiso("REPARACION_CANCELAR"))
            {
                return StatusCode(403, "No tiene permisos para realizar esta acción.");
            }
            try
            {
                if (datos == null || datos.ID_Reparacion <= 0)
                {
                    return BadRequest("La reparación es requerida.");
                }
                if (string.IsNullOrWhiteSpace(datos.Motivo))
                {
                    return BadRequest("Debe indicar el motivo de la cancelación.");
                }
                var result = _accesoDatos.Reparacion_Cancelar(datos);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ErroresApi.Mensaje(ex));
            }
        }

        [HttpPut]
        [Route("ActualizarCostoPrecioReparacion")]
        [RequierePermiso(PermisosApi.MantReparacion)]
        public ActionResult<ReparacionUpdate_return> ActualizarCostoPrecioReparacion([FromBody] ReparacionPendienteUpdateDTO datos)
        {
            try
            {
                var result = AD.ActualizarCostoPrecioReparacion(datos);
                if (result == null)
                {
                    return NotFound();
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ErroresApi.Mensaje(ex));
            }
        }
    }

}
