using Microsoft.AspNetCore.Mvc;
using MVSystemApi.Interfaz;
using MVSystemApi.Model;
using Rotativa.AspNetCore;

namespace MVSystemApi.Controllers
{
    [ApiController]
    public class FacturacionReporteController : Controller
    {
        private readonly Clientes_Negocio _clientes_Negocio;
        private readonly Catalogos_Negocio _catalogos_Negocio;
        private readonly IAccesoDatos _accesoDatos;

        public FacturacionReporteController(IAccesoDatos accesoDatos, Clientes_Negocio clientes_Negocio, Catalogos_Negocio catalogos_Negocio)
        {
            _accesoDatos = accesoDatos;
            _catalogos_Negocio = catalogos_Negocio;
            _clientes_Negocio = clientes_Negocio;
        }

        [HttpPost]
        [Route("FacturacionReporte/PostFactura")]
        public IActionResult Index(Facturas factura)
        {
            try
            {
                if (factura.Cliente != null)
                    factura.IdCliente = _clientes_Negocio.Cliente_Insert(factura.Cliente).ID_Cliente;

                var result = _accesoDatos.PostFactura(factura);
                if (result == null)
                    return NotFound();

                var cliente = _clientes_Negocio.Cliente_Consulta_Por_Id_Cliente(factura.IdCliente);
                var vendedor = _catalogos_Negocio.GetVendedor_Lista().Where(x => x.ID_Vendedor == factura.IdVendedor).Select(x => x.Nombres).FirstOrDefault();

                var data = new FacturaCliente
                {
                    Cliente = cliente,
                    Factura = factura,
                    VendedorNombre = vendedor,
                };

                return new ViewAsPdf(data);
            }
            catch (Exception ex)
            {
                return Ok(ex);
            }
        }
    }
}
