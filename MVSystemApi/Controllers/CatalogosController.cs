using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MVSystemApi.Model;

namespace MVSystemApi.Controllers
{
    [Route("Catalogos")]
    [ApiController]
    public class CatalogosController : ControllerBase
    {
        private readonly Catalogos_Negocio AD;
        public CatalogosController(Catalogos_Negocio _ad)
        {
            AD = _ad;
        }
       
        [HttpGet()]
        [Route("Tipos_Pagos_Combo")]
        public ActionResult<Vendedores_Combo> Tipos_Pagos_Combo()
        {

            try
            {
                var lista = AD.GetTipos_Pagos_Lista();
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
        [Route("Tipos_Factura_Combo")]
        public ActionResult<Vendedores_Combo> Tipos_Factura_Combo()
        {

            try
            {
                var lista = AD.GetTipos_Factura_Lista();
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
        [Route("Accesorios_Facturacion_Lista")]
        public ActionResult<Vendedores_Combo> Accesorios_Facturacion_Lista(int? Id_accesorio, int? id_sucu)
        {

            try
            {
                var lista = AD.GetAccesorios_Facturacion_Lista(Id_accesorio,id_sucu);
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
        [Route("Vendedores_Combo")]
        public ActionResult<Vendedores_Combo> Vendedores_Combo()
        {

            try
            {
                var lista = AD.GetVendedor_Lista();
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
        [Route("Tipo_Cliente_Combo")]
        public ActionResult<TipoClientes> Tipo_Cliente_Combo()
        {

            try
            {
                var lista = AD.GetTipo_Cliente_Lista();
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
        [Route("Tipo_Suplidor_Combo")]
        public ActionResult<TipoClientes> Tipo_Suplidor_Combo()
        {

            try
            {
                var lista = AD.GetTipo_Suplidor_Lista();
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
        [Route("Marcas_Combo")]
        public ActionResult<Marcas> Marcas_Combo()
        {

            try
            {
                var lista = AD.GetMarcas_Combo();
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
        [Route("Tipo_Telefono_Combo")]
        public ActionResult<Telefonos_Tipo> Tipo_Telefono_Combo()
        {

            try
            {
                var lista = AD.GetTipo_Telefono_Lista();
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

        // -------------------- Catalogos para registro de equipos -------------------------------------------------

        [HttpGet]
        [Route("Garantias_Combo")]
        public ActionResult<Garantias> Get_Garantias_Combo()
        {
            try
            {
                var result = AD.Get_Garantias_Combo();
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
        [Route("Tecnologias_Combo")]
        public ActionResult<Tecnologias> Get_Tecnologia()
        {
            try
            {
                var result = AD.GetTecnologia_Combo();
                if (result==null)
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
        [Route("Desbloqueado_Combo")]
        public ActionResult<Desbloqueado> GetDesbloqueado_Combo()
        {
            try
            {
                var result = AD.GetDesbloqueado_Combo();
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
        [Route("Condicion_Equipo_Combo")]
        public ActionResult<Condicion_Equipo> GetCondicion_Equipo()
        {
            try
            {
                var result = AD.GetCondicion_Equipo_Combo();
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
        [Route("Suplidor_Combo")]
        public ActionResult<Suplidor_Combo> GetSuplidor_Equipo()
        {
            try
            {
                var result = AD.Get_Suplidor_Combo();
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
        [Route("Modelos_Combo")]
        public ActionResult<Modelos> GetModelos_Combo(int? ID_Marca)
        {
            try
            {
                var result = AD.GetModelos_Combo(ID_Marca);
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
        [Route("Almacen_Combo")]
        public ActionResult<Modelos> GetAlmacen_Combo()
        {
            try
            {
                var result = AD.GetAlmacen_Combo();
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
        [Route("GetComprobantes_Combo")]
        public ActionResult<Comprobantes> GetComprobantes_Combo()
        {
            try
            {
                var result = AD.GetComprobantes_Combo();
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
        [Route("AsumeITBIS")]
        public IActionResult AsumeITBIS()
        {
            try
            {
                var result = AD.AsumeITBIS();
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