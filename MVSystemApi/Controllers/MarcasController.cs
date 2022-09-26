﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MVSystemApi.Model;
using MVSystemApi.Model_Negocio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MVSystemApi.Controllers
{
    [Route("Marcas")]
    [ApiController]
    public class MarcasController : ControllerBase
    {
        private readonly Marcas_Negocio AD;


        public MarcasController(Marcas_Negocio _cn)
        {
            AD = _cn;
        }

        [HttpPost()]
        [Route("Marca_Insert")]
        [Authorize("MNU_MANT_MARCAS")]
        public ActionResult<Marcas> Marca_Insert([FromBody] Marcas Marca)
        {

            try
            {
                var lista = AD.Marca_Insert(Marca);
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
