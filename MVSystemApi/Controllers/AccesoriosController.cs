using DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
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
        // Permiso (seg.Permisos) para la vista "Movimientos" de la consulta de accesorios. Llega en el token como claim "role".
        private const string PermisoMovimientos = "ACCESORIOS_MOVIMIENTOS";

        private bool TienePermiso(string permiso)
        {
            return User.Claims.Any(claim =>
                (claim.Type == "role" || claim.Type == System.Security.Claims.ClaimTypes.Role) && claim.Value == permiso);
        }

        /// <summary>
        /// Movimientos de stock de accesorios (entradas, ajustes, ventas, cambios de pieza...), paginados, con totales por motivo.
        /// GET: Accesorios/Accesorio_Movimientos?fechaInicio=&amp;fechaFinal=&amp;sucursal=&amp;filtro=&amp;motivo=&amp;usuario=&amp;pageIndex=&amp;pageSize=
        /// </summary>
        [HttpGet]
        [Route("Accesorio_Movimientos")]
        [Authorize]
        public ActionResult Accesorio_Movimientos([FromQuery] DateTime fechaInicio, [FromQuery] DateTime fechaFinal, [FromQuery] int sucursal,
            [FromQuery] string filtro, [FromQuery] string motivo, [FromQuery] string usuario,
            [FromQuery] int pageIndex = 1, [FromQuery] int pageSize = 20)
        {
            if (!TienePermiso(PermisoMovimientos))
            {
                return StatusCode(StatusCodes.Status403Forbidden, "No tiene permisos para realizar esta acción.");
            }
            try
            {
                var filas = accesoDatos.Accesorio_Movimientos_Consultar(fechaInicio, fechaFinal, sucursal, filtro, motivo, usuario, pageIndex, pageSize).AsEnumerable()
                    .Select(dt => new
                    {
                        fecha = Convert.ToDateTime(dt["Fecha"]),
                        motivo = Convert.ToString(dt["Motivo"]),
                        documento = dt["Documento"] == DBNull.Value ? null : Convert.ToString(dt["Documento"]),
                        idAccesorio = Convert.ToInt64(dt["ID_Accesorio"]),
                        descripcion = Convert.ToString(dt["Descripcion_Accesorio"]),
                        modelo = Convert.ToString(dt["Modelo"]),
                        idAlmacen = Convert.ToInt32(dt["ID_Almacen"]),
                        almacen = dt["Descripcion_Almacen"] == DBNull.Value ? null : Convert.ToString(dt["Descripcion_Almacen"]),
                        entrada = Convert.ToInt32(dt["Entrada"]),
                        salida = Convert.ToInt32(dt["Salida"]),
                        saldo = dt["Saldo"] == DBNull.Value ? (int?)null : Convert.ToInt32(dt["Saldo"]),
                        usuario = dt["Usuario"] == DBNull.Value ? null : Convert.ToString(dt["Usuario"]),
                        nota = dt["Nota"] == DBNull.Value ? null : Convert.ToString(dt["Nota"]),
                        origen = Convert.ToString(dt["Origen"]),
                        totalRegistros = Convert.ToInt32(dt["Total_Registros"])
                    }).ToList();

                var resumen = accesoDatos.Accesorio_Movimientos_Resumen(fechaInicio, fechaFinal, sucursal, filtro, motivo, usuario).AsEnumerable()
                    .Select(dt => new
                    {
                        motivo = Convert.ToString(dt["Motivo"]),
                        movimientos = Convert.ToInt32(dt["Movimientos"]),
                        entradas = Convert.ToInt32(dt["Entradas"]),
                        salidas = Convert.ToInt32(dt["Salidas"])
                    }).ToList();

                return Ok(new { total = filas.Count == 0 ? 0 : filas[0].totalRegistros, items = filas, resumen });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>Accesorios existentes con el mismo modelo y descripcion en el almacen (antes de registrar uno nuevo).</summary>
        [HttpGet]
        [Route("Accesorio_Coincidencias")]
        public ActionResult Accesorio_Coincidencias([FromQuery] int idModelo, [FromQuery] string descripcion, [FromQuery] int idAlmacen)
        {
            try
            {
                var lista = accesoDatos.Accesorio_Coincidencias(idModelo, descripcion, idAlmacen).AsEnumerable()
                    .Select(dt => new
                    {
                        idAccesorio = Convert.ToInt64(dt["ID_Accesorio"]),
                        descripcion = Convert.ToString(dt["Descripcion_Accesorio"]),
                        modelo = Convert.ToString(dt["Modelo"]),
                        idAlmacen = Convert.ToInt32(dt["ID_Almacen"]),
                        almacen = Convert.ToString(dt["Descripcion_Almacen"]),
                        cantidad = Convert.ToInt32(dt["Cantidad"]),
                        costoEquipo = Convert.ToDecimal(dt["Costo_Equipo"]),
                        precioPorMayor = Convert.ToDecimal(dt["Precio_Por_Mayor"]),
                        precioDetalle = Convert.ToDecimal(dt["Precio_Detalle"]),
                        estado = Convert.ToString(dt["Estado"]),
                        codigoBarra = dt["Codigo_Barra"] == DBNull.Value ? (long?)null : Convert.ToInt64(dt["Codigo_Barra"])
                    }).ToList();
                return Ok(lista);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        /// <summary>Suma cantidad a un accesorio que ya existe (en lugar de registrarlo de nuevo).</summary>
        [HttpPost]
        [Route("Accesorio_Sumar_Existencia")]
        public ActionResult Accesorio_Sumar_Existencia([FromBody] AccesorioSumarDTO datos)
        {
            try
            {
                var r = accesoDatos.Accesorio_Sumar_Existencia(datos).AsEnumerable()
                    .Select(dt => new
                    {
                        idAccesorio = Convert.ToInt64(dt["ID_Accesorio"]),
                        descripcion = Convert.ToString(dt["Descripcion_Accesorio"]),
                        cantidad = Convert.ToInt32(dt["Cantidad"]),
                        mensaje = Convert.ToString(dt["Mensaje"])
                    }).FirstOrDefault();
                return Ok(r);
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
                var lista = AD.GetAccesorios(paginate,accesorio,almacen);

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
        public ActionResult<List<Accesorio>> GetAccesoriosVendidos([FromQuery] Paginate paginate, string filter, int almacen)
        {

            try
            {
                var lista = AD.GetAccesoriosVendidos(paginate, filter, almacen);

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
