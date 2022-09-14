using DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MVSystemApi.Interfaz;
using MVSystemApi.Model;
using Rotativa.AspNetCore;

namespace MVSystemApi.Controllers
{
    [Route("EquipoDisponibleReporte")]
    [ApiController]
    public class EquipoDisponibleReporteController : ControllerBase
    {
        private readonly Equipos_Negocio _equiposNegocio;
        public EquipoDisponibleReporteController(Equipos_Negocio equiposNegocio)
        {
            _equiposNegocio = equiposNegocio;
        
        }

        [HttpPost]
        [Route("/GetEquiposDisponible")]
        public IActionResult Index([FromQuery] EquipoDisponibleFilterDTO equipoDisponibleFilterDTO)
        {


            var result = _equiposNegocio.GetEquiposDisponible(equipoDisponibleFilterDTO);
            if (result == null)
                return NotFound();



            return new ViewAsPdf(result);
        }
    }
}
