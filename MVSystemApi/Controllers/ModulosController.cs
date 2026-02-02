using DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MVSystemApi.Model_Negocio.Seguridad;

namespace MVSystemApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ModulosController : ControllerBase
    {
        private readonly ModulosService _modulosService;

        public ModulosController(ModulosService modulosService)
        {
            _modulosService = modulosService;
        }

        /// <summary>
        /// Obtiene todos los módulos
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ModuloResponseDTO>>> ObtenerTodos()
        {
            try
            {
                var modulos = await _modulosService.ObtenerTodos();
                return Ok(modulos);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene un módulo por código
        /// </summary>
        [HttpGet("{codigo}")]
        public async Task<ActionResult<ModuloResponseDTO>> ObtenerPorCodigo(int codigo)
        {
            try
            {
                var modulo = await _modulosService.ObtenerPorCodigo(codigo);
                if (modulo == null)
                    return NotFound(new { error = "Módulo no encontrado" });

                return Ok(modulo);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene los módulos de un sistema específico
        /// </summary>
        [HttpGet("por-sistema/{codigoSistema}")]
        public async Task<ActionResult<IEnumerable<ModuloResponseDTO>>> ObtenerPorSistema(int codigoSistema)
        {
            try
            {
                var modulos = await _modulosService.ObtenerPorSistema(codigoSistema);
                return Ok(modulos);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Crea un nuevo módulo
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> Crear([FromBody] CrearModuloDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var codigo = await _modulosService.Crear(dto);
                return CreatedAtAction(nameof(ObtenerPorCodigo), new { codigo }, new { id = codigo });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Actualiza un módulo existente
        /// </summary>
        [HttpPut("{codigo}")]
        public async Task<ActionResult> Actualizar(int codigo, [FromBody] ActualizarModuloDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                dto.Codigo = codigo;
                var resultado = await _modulosService.Actualizar(dto);
                
                if (!resultado)
                    return NotFound(new { error = "Módulo no encontrado" });

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Elimina un módulo
        /// </summary>
        [HttpDelete("{codigo}")]
        public async Task<ActionResult> Eliminar(int codigo)
        {
            try
            {
                var resultado = await _modulosService.Eliminar(codigo);
                if (!resultado)
                    return NotFound(new { error = "Módulo no encontrado" });

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
