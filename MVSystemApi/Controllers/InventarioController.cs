using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MVSystemApi.Model;
using MVSystemApi.Model_Negocio;

namespace MVSystemApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InventarioController : ControllerBase
    {
        private readonly Inventario_Negocio _inventarioNegocio;

        public InventarioController(Inventario_Negocio inventarioNegocio)
        {
            _inventarioNegocio = inventarioNegocio;
        }

        /// <summary>
        /// Inicia una nueva sesión de inventario para un almacén
        /// POST: api/Inventario/IniciarSesion
        /// </summary>
        [HttpPost("IniciarSesion")]
       // [Authorize(Roles = "MNU_MANT_PASARINVEQ")]
        public ActionResult<IniciarInventarioResponse> IniciarSesion([FromBody] IniciarInventarioDTO dto)
        {
            try
            {
                var result = _inventarioNegocio.IniciarSesion(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Registra el escaneo de un equipo
        /// POST: api/Inventario/RegistrarEscaneo
        /// </summary>
        [HttpPost("RegistrarEscaneo")]
        //[Authorize(Roles = "MNU_MANT_PASARINVEQ")]
        public ActionResult<EscaneoResponse> RegistrarEscaneo([FromBody] RegistrarEscaneoDTO dto)
        {
            try
            {
                var result = _inventarioNegocio.RegistrarEscaneo(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene equipos pendientes por escanear
        /// GET: api/Inventario/Pendientes/123
        /// </summary>
        [HttpGet("Pendientes/{idSesion}")]
       // [Authorize(Roles = "MNU_MANT_PASARINVEQ")]
        public ActionResult<List<InventarioEquipo>> ObtenerPendientes(long idSesion)
        {
            try
            {
                var result = _inventarioNegocio.ObtenerPendientes(idSesion);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene equipos ya escaneados
        /// GET: api/Inventario/Escaneados/123
        /// </summary>
        [HttpGet("Escaneados/{idSesion}")]
       // [Authorize(Roles = "MNU_MANT_PASARINVEQ")]
        public ActionResult<List<InventarioEquipo>> ObtenerEscaneados(long idSesion)
        {
            try
            {
                var result = _inventarioNegocio.ObtenerEscaneados(idSesion);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

            /// <summary>
            /// Obtiene equipos faltantes por sesión usando SP
            /// GET: api/Inventario/Faltantes/{idSesion}
            /// </summary>
            [HttpGet("Faltantes/{idSesion}")]
            public ActionResult<List<InventarioEquipo>> ObtenerFaltantes(long idSesion)
            {
                try
                {
                    var equipos = _inventarioNegocio.ObtenerEquiposFaltantes(idSesion);
                    return Ok(equipos);
                }
                catch (Exception ex)
                {
                    return BadRequest(new { message = ex.Message });
                }
            }

            /// <summary>
            /// Obtiene equipos sobrantes por sesión usando SP
            /// GET: api/Inventario/Sobrantes/{idSesion}
            /// </summary>
            [HttpGet("Sobrantes/{idSesion}")]
            public ActionResult<List<InventarioEquipo>> ObtenerSobrantes(long idSesion)
            {
                try
                {
                    var equipos = _inventarioNegocio.ObtenerEquiposSobrantes(idSesion);
                    return Ok(equipos);
                }
                catch (Exception ex)
                {
                    return BadRequest(new { message = ex.Message });
                }
            }

        /// <summary>
        /// Finaliza el inventario y actualiza estados de equipos
        /// POST: api/Inventario/Finalizar
        /// </summary>
        [HttpPost("Finalizar")]
       // [Authorize(Roles = "MNU_MANT_PASARINVEQ")]
        public ActionResult<FinalizarInventarioResponse> FinalizarInventario([FromBody] FinalizarInventarioDTO dto)
        {
            try
            {
                var result = _inventarioNegocio.FinalizarInventario(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene información de una sesión de inventario
        /// GET: api/Inventario/Sesion/123
        /// </summary>
        [HttpGet("Sesion/{idSesion}")]
      //  [Authorize(Roles = "MNU_MANT_PASARINVEQ,consultaInventarioToolStripMenuItem")]
        public ActionResult<InventarioSesion> ObtenerSesion(long idSesion)
        {
            try
            {
                var result = _inventarioNegocio.ObtenerSesion(idSesion);
                
                if (result == null)
                {
                    return NotFound(new { message = "Sesión de inventario no encontrada" });
                }
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Pausa un inventario en proceso
        /// POST: api/Inventario/Pausar
        /// </summary>
        [HttpPost("Pausar")]
       // [Authorize(Roles = "MNU_MANT_PASARINVEQ")]
        public ActionResult PausarInventario([FromBody] GestionarInventarioDTO dto)
        {
            try
            {
                bool resultado = _inventarioNegocio.PausarInventario(dto.ID_Sesion, dto.Usuario);
                if (resultado)
                {
                    return Ok(new { message = "Inventario pausado correctamente" });
                }
                else
                {
                    return BadRequest(new { message = "No se pudo pausar el inventario. Verifique que el inventario esté en proceso." });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Reanuda un inventario pausado
        /// POST: api/Inventario/Reanudar
        /// </summary>
        [HttpPost("Reanudar")]
       // [Authorize(Roles = "MNU_MANT_PASARINVEQ")]
        public ActionResult ReanudarInventario([FromBody] GestionarInventarioDTO dto)
        {
            try
            {
                bool resultado = _inventarioNegocio.ReanudarInventario(dto.ID_Sesion, dto.Usuario);
                if (resultado)
                {
                    return Ok(new { message = "Inventario reanudado correctamente" });
                }
                else
                {
                    return BadRequest(new { message = "No se pudo reanudar el inventario. Verifique que el inventario esté pausado." });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene inventarios activos (en proceso o pausados)
        /// GET: api/Inventario/Activos
        /// GET: api/Inventario/Activos?idAlmacen=5
        /// </summary>
        [HttpGet("Activos")]
       // [Authorize(Roles = "MNU_MANT_PASARINVEQ,consultaInventarioToolStripMenuItem")]
        public ActionResult<List<InventarioSesion>> ObtenerInventariosActivos([FromQuery] int? idAlmacen = null)
        {
            try
            {
                var result = _inventarioNegocio.ObtenerInventariosActivos(idAlmacen);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// TEMPORAL: Cancela un inventario activo (para desarrollo/pruebas)
        /// DELETE: api/Inventario/Cancelar/123
        /// </summary>
        [HttpDelete("Cancelar/{idSesion}")]
        public ActionResult CancelarInventario(long idSesion)
        {
            try
            {
                _inventarioNegocio.CancelarInventario(idSesion);
                return Ok(new { message = "Inventario cancelado correctamente" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

    }
}
