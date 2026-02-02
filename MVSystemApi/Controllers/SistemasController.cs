using DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MVSystemApi.Model_Negocio.Seguridad;

namespace MVSystemApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SistemasController : ControllerBase
    {
        private readonly SistemasService _sistemasService;

        public SistemasController(SistemasService sistemasService)
        {
            _sistemasService = sistemasService;
        }

        /// <summary>
        /// Obtiene todos los sistemas
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SistemaResponseDTO>>> ObtenerTodos()
        {
            try
            {
                var sistemas = await _sistemasService.ObtenerTodos();
                return Ok(sistemas);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene un sistema por código
        /// </summary>
        [HttpGet("{codigo}")]
        public async Task<ActionResult<SistemaResponseDTO>> ObtenerPorCodigo(int codigo)
        {
            try
            {
                var sistema = await _sistemasService.ObtenerPorCodigo(codigo);
                if (sistema == null)
                    return NotFound(new { error = "Sistema no encontrado" });

                return Ok(sistema);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Crea un nuevo sistema
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> Crear([FromBody] CrearSistemaDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var codigo = await _sistemasService.Crear(dto);
                return CreatedAtAction(nameof(ObtenerPorCodigo), new { codigo }, new { id = codigo });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Actualiza un sistema existente
        /// </summary>
        [HttpPut("{codigo}")]
        public async Task<ActionResult> Actualizar(int codigo, [FromBody] ActualizarSistemaDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                dto.Codigo = codigo;
                var resultado = await _sistemasService.Actualizar(dto);
                
                if (!resultado)
                    return NotFound(new { error = "Sistema no encontrado" });

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Elimina un sistema
        /// </summary>
        [HttpDelete("{codigo}")]
        public async Task<ActionResult> Eliminar(int codigo)
        {
            try
            {
                var resultado = await _sistemasService.Eliminar(codigo);
                if (!resultado)
                    return NotFound(new { error = "Sistema no encontrado" });

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
