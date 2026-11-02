using DTO;
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
