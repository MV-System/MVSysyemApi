using Microsoft.AspNetCore.Mvc;
using MVSystemApi.Interfaz;
using MVSystemApi.Model;
using MVSystemApi.Model_Negocio;
using Newtonsoft.Json;
using Rotativa.AspNetCore;

namespace MVSystemApi.Controllers
{
    [ApiController]
    public class FacturacionReporteController : Controller
    {
        private readonly Clientes_Negocio _clientes_Negocio;
        private readonly Catalogos_Negocio _catalogos_Negocio;
        private readonly Facturas_Negocio facturasNegocio;
        private readonly IAccesoDatos _accesoDatos;

        public FacturacionReporteController(IAccesoDatos accesoDatos, Clientes_Negocio clientes_Negocio, Catalogos_Negocio catalogos_Negocio,Facturas_Negocio facturasNegocio)
        {
            _accesoDatos = accesoDatos;
            _catalogos_Negocio = catalogos_Negocio;
            _clientes_Negocio = clientes_Negocio;
            this.facturasNegocio = facturasNegocio;
        }

        [HttpPost]
        [Route("FacturacionReporte/PostFactura")]
        public IActionResult RptFactura(Facturas factura)
        {
            if (factura.Cliente != null)
                factura.IdCliente = _clientes_Negocio.Cliente_Consulta_Por_Id_Cliente(factura.IdCliente).Id_Cliente;

            var result = _accesoDatos.PostFactura(factura);
            if (result == null)
                return NotFound();

            factura.TipoPago = ((List<Tipos_Pagos>)_catalogos_Negocio
                    .GetTipos_Pagos_Lista())
                    .Where(x => x.ID_Tipo_Pagos == factura.IdTipoPago)
                    .Select(p => p.Descripcion_Tipo_Pagos)
                    .FirstOrDefault();
            factura.TipoFactura = ((List<Tipos_Facturas>)_catalogos_Negocio
                  .GetTipos_Factura_Lista())
                  .Where(x => x.ID_Tipo_Factura == factura.IdTipoFactura)
                  .Select(p => p.Descripcion_Factura_Tipo)
                  .FirstOrDefault();
            var facturaReporte = _accesoDatos.ObtenerFacturaReporte(result.NumeroFactura, 1);
            var cliente = _clientes_Negocio.Cliente_Consulta_Por_Id_Cliente(factura.IdCliente);
            var vendedor = _catalogos_Negocio.GetVendedor_Lista().Where(x => x.ID_Vendedor == factura.IdVendedor).Select(x => x.Nombres).FirstOrDefault();

            var data = new FacturaCliente
            {
                Cliente = cliente,
                Factura = factura,
                FacturaReporte = facturaReporte,
                VendedorNombre = vendedor,
            };

            return new ViewAsPdf(data);
        }

        [HttpPost("FacturacionReporte/ReportFacturaConsulta")]
        public IActionResult ReportFacturaConsulta(FacturaFilter consulta)
        {
            var result =(List<FacturaConsulta>) facturasNegocio.GetFacturas(consulta);
            return new ViewAsPdf(result);
        }


        [HttpPost("FacturacionReporte/ReportFactura")]
        public IActionResult ReportFactura(FacturaFilter consulta)
        {
            var detalleFactura = (List<DetalleFactura>)facturasNegocio.GetDetalleFacturaConsulta(consulta.NumeroFactura, consulta.Almacen);
            var results = (List<FacturaConsulta>)facturasNegocio.GetFacturas(consulta);
            var factura = results.Where(x => x.NumeroFactura == consulta.NumeroFactura).FirstOrDefault();
            var data = new Facturas
            {
                DetalleFacturaList = detalleFactura,
                Factura = factura
            };

            return new ViewAsPdf(data);
        }
        [HttpPost("FacturacionReporte/ReportCotizacion")]
        public IActionResult ReportCotizacion(CotizacionModel model)
        {
            return new ViewAsPdf(model);
        }
    }
}
