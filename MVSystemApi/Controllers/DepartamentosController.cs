using DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MVSystemApi.Model_Negocio.Seguridad;

namespace MVSystemApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DepartamentosController : ControllerBase
    {
        private readonly DepartamentosService _departamentosService;

        public DepartamentosController(DepartamentosService departamentosService)
        {
            _departamentosService = departamentosService;
        }

        /// <summary>
        /// Obtiene todos los departamentos
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DepartamentoResponseDTO>>> ObtenerTodos()
        {
            try
            {
                var departamentos = await _departamentosService.ObtenerTodos();
                return Ok(departamentos);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene un departamento por código
        /// </summary>
        [HttpGet("{codigo}")]
        public async Task<ActionResult<DepartamentoResponseDTO>> ObtenerPorCodigo(int codigo)
        {
            try
            {
                var departamento = await _departamentosService.ObtenerPorCodigo(codigo);
                if (departamento == null)
                    return NotFound(new { error = "Departamento no encontrado" });

                return Ok(departamento);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Crea un nuevo departamento
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> Crear([FromBody] CrearDepartamentoDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var codigo = await _departamentosService.Crear(dto);
                return CreatedAtAction(nameof(ObtenerPorCodigo), new { codigo }, new { id = codigo });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Actualiza un departamento existente
        /// </summary>
        [HttpPut("{codigo}")]
        public async Task<ActionResult> Actualizar(int codigo, [FromBody] ActualizarDepartamentoDTO dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                dto.Codigo = codigo;
                var resultado = await _departamentosService.Actualizar(dto);
                
                if (!resultado)
                    return NotFound(new { error = "Departamento no encontrado" });

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Elimina un departamento
        /// </summary>
        [HttpDelete("{codigo}")]
        public async Task<ActionResult> Eliminar(int codigo)
        {
            try
            {
                var resultado = await _departamentosService.Eliminar(codigo);
                if (!resultado)
                    return NotFound(new { error = "Departamento no encontrado" });

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
