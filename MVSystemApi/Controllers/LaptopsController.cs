using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MVSystemApi.Interfaz;
using MVSystemApi.Model;

namespace MVSystemApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LaptopsController : ControllerBase
    {
        private readonly IAccesoDatos _accesoDatos;
        private readonly int _horasMaxCustodia;
        private readonly bool _bloquearVencidos;

        // Permisos del módulo (seg.Permisos). Llegan en el token como claims "role".
        private const string PermisoCambioPieza = "LAPTOP_CAMBIO_PIEZA";
        private const string PermisoCustodiaConsulta = "LAPTOP_CUSTODIA_CONSULTA";
        private const string PermisoCustodiaRecibir = "LAPTOP_CUSTODIA_RECIBIR";
        private const string PermisoCorregirConfig = "LAPTOP_CORREGIR_CONFIG";
        private const string PermisoCambioAnular = "LAPTOP_CAMBIO_ANULAR";

        public LaptopsController(IAccesoDatos accesoDatos, IConfiguration configuration)
        {
            _accesoDatos = accesoDatos;
            _horasMaxCustodia = configuration.GetValue("Laptops:HorasMaxCustodia", 24);
            _bloquearVencidos = configuration.GetValue("Laptops:BloquearTecnicoConPendientesVencidos", false);
        }

        /// <summary>True si el usuario tiene al menos uno de los permisos indicados.</summary>
        private bool TienePermiso(params string[] permisos)
        {
            return User.Claims.Any(claim =>
                (claim.Type == "role" || claim.Type == ClaimTypes.Role) && permisos.Contains(claim.Value));
        }

        /// <summary>Usuario del token (claim name); nunca el que llegue en el body.</summary>
        private string UsuarioToken()
        {
            var usuario = (User.Identity?.Name ?? User.FindFirst("name")?.Value)?.Trim();
            return string.IsNullOrEmpty(usuario) ? null : usuario;
        }

        private ActionResult SinUsuario()
        {
            return StatusCode(StatusCodes.Status401Unauthorized, "No se pudo identificar al usuario de la sesión.");
        }

        private ActionResult SinPermiso()
        {
            return StatusCode(StatusCodes.Status403Forbidden, "No tiene permisos para realizar esta acción.");
        }

        /// <summary>
        /// Límite de horas de custodia, si se bloquea al técnico con pendientes vencidos y el % del costo de una pieza usada (appsettings).
        /// GET: api/Laptops/Custodia/Configuracion
        /// </summary>
        [HttpGet("Custodia/Configuracion")]
        public ActionResult CustodiaConfiguracion()
        {
            if (!TienePermiso(PermisoCambioPieza, PermisoCustodiaConsulta)) { return SinPermiso(); }
            return Ok(new { horasMax = _horasMaxCustodia, bloquearVencidos = _bloquearVencidos });
        }

        /// <summary>
        /// Guarda (upsert) la configuración de una laptop.
        /// POST: api/Laptops/Configuracion
        /// </summary>
        [HttpPost("Configuracion")]
        public ActionResult Configuracion_Guardar([FromBody] EquipoConfiguracion configuracion)
        {
            try
            {
                if (configuracion == null || string.IsNullOrWhiteSpace(configuracion.Imei))
                {
                    return BadRequest("El serial/IMEI es requerido.");
                }
                var result = _accesoDatos.Equipo_Configuracion_Guardar(configuracion);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Consulta la configuración de una laptop.
        /// GET: api/Laptops/Configuracion?imei=
        /// </summary>
        [HttpGet("Configuracion")]
        public ActionResult Configuracion_Consultar([FromQuery] string imei)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(imei))
                {
                    return BadRequest("El serial/IMEI es requerido.");
                }
                var result = _accesoDatos.Equipo_Configuracion_Consultar(imei);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Módulos (memorias y discos) instalados en una laptop.
        /// GET: api/Laptops/Configuracion/Modulos?imei=
        /// </summary>
        [HttpGet("Configuracion/Modulos")]
        public ActionResult Configuracion_Modulos([FromQuery] string imei)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(imei))
                {
                    return BadRequest("El serial/IMEI es requerido.");
                }
                return Ok(_accesoDatos.Equipo_Configuracion_Modulos_Consultar(imei));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Modelos de componentes (RAM / discos) con su tipo, para elegir el modelo de cada módulo.
        /// GET: api/Laptops/Configuracion/ModelosComponente
        /// </summary>
        [HttpGet("Configuracion/ModelosComponente")]
        public ActionResult Configuracion_ModelosComponente()
        {
            try
            {
                return Ok(_accesoDatos.Componente_Modelos_Consultar());
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Correcciones manuales de los módulos de una laptop.
        /// GET: api/Laptops/Configuracion/Correcciones?imei=
        /// </summary>
        [HttpGet("Configuracion/Correcciones")]
        public ActionResult Configuracion_Correcciones([FromQuery] string imei)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(imei))
                {
                    return BadRequest("El serial/IMEI es requerido.");
                }
                return Ok(_accesoDatos.Equipo_Configuracion_Correcciones_Consultar(imei));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Corrige el modelo y/o la capacidad de un módulo instalado (sin mover inventario ni costo). Requiere motivo.
        /// POST: api/Laptops/Configuracion/Modulos/Corregir
        /// </summary>
        [HttpPost("Configuracion/Modulos/Corregir")]
        public ActionResult Configuracion_Modulo_Corregir([FromBody] EquipoModuloCorreccion correccion)
        {
            if (!TienePermiso(PermisoCorregirConfig)) { return SinPermiso(); }
            try
            {
                if (correccion == null || correccion.ID_Modulo <= 0)
                {
                    return BadRequest("El módulo es requerido.");
                }
                correccion.Usuario = UsuarioToken();
                if (correccion.Usuario == null) { return SinUsuario(); }
                var result = _accesoDatos.Equipo_Configuracion_Modulo_Corregir(correccion);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Registra el cambio de una pieza (RAM / almacenamiento) de una laptop.
        /// POST: api/Laptops/CambioPieza
        /// </summary>
        [HttpPost("CambioPieza")]
        public ActionResult CambioPieza([FromBody] EquipoCambioPieza cambio)
        {
            if (!TienePermiso(PermisoCambioPieza)) { return SinPermiso(); }
            try
            {
                if (cambio == null || string.IsNullOrWhiteSpace(cambio.Imei))
                {
                    return BadRequest("El serial/IMEI es requerido.");
                }
                // El tecnico es siempre quien registra el cambio (token), no lo que llegue en el body
                cambio.Usuario = UsuarioToken();
                if (cambio.Usuario == null) { return SinUsuario(); }
                cambio.Tecnico = cambio.Usuario;
                var result = _accesoDatos.Equipo_Cambio_Pieza_Registrar(cambio);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Anula un cambio de pieza (el último de la laptop, con la pieza retirada aún sin recibir). Requiere motivo.
        /// POST: api/Laptops/CambioPieza/Anular
        /// </summary>
        [HttpPost("CambioPieza/Anular")]
        public ActionResult CambioPiezaAnular([FromBody] EquipoCambioAnulacion anulacion)
        {
            if (!TienePermiso(PermisoCambioAnular)) { return SinPermiso(); }
            try
            {
                if (anulacion == null || anulacion.ID_Cambio <= 0)
                {
                    return BadRequest("El cambio de pieza es requerido.");
                }
                anulacion.Usuario = UsuarioToken();
                if (anulacion.Usuario == null) { return SinUsuario(); }
                var result = _accesoDatos.Equipo_Cambio_Pieza_Anular(anulacion);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Confirma la recepción de la pieza retirada en custodia (otro usuario distinto al técnico).
        /// POST: api/Laptops/Custodia/Recibir
        /// </summary>
        [HttpPost("Custodia/Recibir")]
        public ActionResult CustodiaRecibir([FromBody] EquipoCustodiaAccion accion)
        {
            if (!TienePermiso(PermisoCustodiaRecibir)) { return SinPermiso(); }
            try
            {
                if (accion == null || accion.ID_Cambio <= 0)
                {
                    return BadRequest("El cambio de pieza es requerido.");
                }
                accion.Usuario = UsuarioToken();
                if (accion.Usuario == null) { return SinUsuario(); }
                var result = _accesoDatos.Equipo_Cambio_Pieza_Recibir(accion);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// La pieza retirada en custodia se la lleva el cliente (se suma su costo a la laptop).
        /// POST: api/Laptops/Custodia/EntregarCliente
        /// </summary>
        [HttpPost("Custodia/EntregarCliente")]
        public ActionResult CustodiaEntregarCliente([FromBody] EquipoCustodiaAccion accion)
        {
            if (!TienePermiso(PermisoCustodiaRecibir)) { return SinPermiso(); }
            try
            {
                if (accion == null || accion.ID_Cambio <= 0)
                {
                    return BadRequest("El cambio de pieza es requerido.");
                }
                accion.Usuario = UsuarioToken();
                if (accion.Usuario == null) { return SinUsuario(); }
                var result = _accesoDatos.Equipo_Cambio_Pieza_EntregarCliente(accion);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Piezas retiradas pendientes en custodia, con horas transcurridas.
        /// GET: api/Laptops/Custodia/Pendientes?tecnico=&idAlmacen=&horasMax=
        /// </summary>
        [HttpGet("Custodia/Pendientes")]
        public ActionResult CustodiaPendientes([FromQuery] string tecnico, [FromQuery] int? idAlmacen, [FromQuery] int? horasMax)
        {
            if (!TienePermiso(PermisoCustodiaConsulta, PermisoCambioPieza)) { return SinPermiso(); }
            try
            {
                var result = _accesoDatos.Equipo_Cambio_Pieza_Pendientes(tecnico, idAlmacen, horasMax ?? _horasMaxCustodia);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Datos de la laptop para la pantalla de cambio de pieza.
        /// GET: api/Laptops/Equipo?imei=
        /// </summary>
        [HttpGet("Equipo")]
        public ActionResult Equipo([FromQuery] string imei)
        {
            if (!TienePermiso(PermisoCambioPieza)) { return SinPermiso(); }
            try
            {
                if (string.IsNullOrWhiteSpace(imei))
                {
                    return BadRequest("El serial/IMEI es requerido.");
                }
                var result = _accesoDatos.Equipo_Cambio_Pieza_Equipo_Consultar(imei);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Accesorios (Componentes) de un almacén para elegir pieza instalada/retirada.
        /// GET: api/Laptops/Accesorios?idAlmacen=&tipoPieza=&filtro=&soloConStock=
        /// </summary>
        [HttpGet("Accesorios")]
        public ActionResult Accesorios([FromQuery] int idAlmacen, [FromQuery] string tipoPieza, [FromQuery] string filtro, [FromQuery] bool soloConStock = false)
        {
            if (!TienePermiso(PermisoCambioPieza)) { return SinPermiso(); }
            try
            {
                var result = _accesoDatos.Equipo_Cambio_Pieza_Accesorios(idAlmacen, tipoPieza, filtro, soloConStock);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Historial de cambios de pieza de una laptop.
        /// GET: api/Laptops/Historial?imei=
        /// </summary>
        [HttpGet("Historial")]
        public ActionResult Historial([FromQuery] string imei)
        {
            if (!TienePermiso(PermisoCambioPieza, PermisoCustodiaConsulta)) { return SinPermiso(); }
            try
            {
                if (string.IsNullOrWhiteSpace(imei))
                {
                    return BadRequest("El serial/IMEI es requerido.");
                }
                var result = _accesoDatos.Equipo_Cambio_Pieza_Historial(imei);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
