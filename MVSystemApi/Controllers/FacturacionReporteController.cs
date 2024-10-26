using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MVSystemApi.Interfaz;
using MVSystemApi.Model;
using MVSystemApi.Model_Negocio;
using MVSystemApi.Model_Negocio.Seguridad;
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
        private readonly SeguridadService _SS;

        public FacturacionReporteController(SeguridadService SS,IAccesoDatos accesoDatos, Clientes_Negocio clientes_Negocio, Catalogos_Negocio catalogos_Negocio,Facturas_Negocio facturasNegocio)
        {
            _accesoDatos = accesoDatos;
            _catalogos_Negocio = catalogos_Negocio;
            _clientes_Negocio = clientes_Negocio;
            this.facturasNegocio = facturasNegocio;
            _SS = SS;
        }

        [HttpPost]
        [Route("FacturacionReporte/RptFactura")]// Generar Factura Nueva
        //[Authorize(Roles = "MNU_MANT_FACTURACION")]
        public IActionResult RptFactura(Facturas factura)
        {
            if (factura.Cliente != null)
                factura.IdCliente = _clientes_Negocio.Cliente_Consulta_Por_Id_Cliente(factura.IdCliente).Id_Cliente;

            var result = _accesoDatos.PostFactura(factura);
            if (result == null)
                return NotFound();
          
            string baseUrl = $"{Request.Scheme}://{Request.Host.Value}"; // Obtener la URL base del servidor
            factura.logo = baseUrl + "/assets/" + factura.empresa + ".png"; // armar la ruta completa para acceder al logo

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

            // Obtener los datos de la empresa (factura) con el IdEmpresa que llega en el request
            DatosEmpresa DatosEmpresaFactura = _SS.GetDatosEmpresa(Convert.ToInt32(factura.IdEmpresa), Convert.ToInt32(factura.IdSucursal));


            // Crear el objeto EmpresaFactura para la vista
            //var data = new EmpresaReportes
            //{
            //    DatosEmpresaFactura = DatosEmpresaFactura,
            //    EquiposDTO = equipoDisponibles,
            //};

            // Devolver la vista PDF con el objeto modificado
           // return new ViewAsPdf(data);

            var data = new FacturaCliente
            {
                Cliente = cliente,
                Factura = factura,
                FacturaReporte = facturaReporte,
                VendedorNombre = vendedor,
                DatosEmpresaFactura = DatosEmpresaFactura,
            };
            data.Factura.logo = factura.logo;
            return new ViewAsPdf(data);
        }

        [HttpPost("FacturacionReporte/ReportFacturaConsulta")]// Imrimir reporte de facturas
        public IActionResult ReportFacturaConsulta(FacturaFilter consulta)
        {
            var result =(List<FacturaConsulta>) facturasNegocio.GetFacturas(consulta);

            string baseUrl = $"{Request.Scheme}://{Request.Host.Value}"; // Obtener la URL base del servidor
            result.ForEach(ft => ft.logo = baseUrl + "/assets/" + consulta.empresa + ".png");

            return new ViewAsPdf(result);
        }


        [HttpPost("FacturacionReporte/ReportFactura")]// Reimprimir factura 8.5x11
        public IActionResult ReportFactura(FacturaFilter consulta)
        {
               var detalleFactura = (List<DetalleFactura>)facturasNegocio.GetDetalleFacturaConsulta(consulta.NumeroFactura, consulta.Almacen);
            
            var results = (List<FacturaConsulta>)facturasNegocio.GetFacturasReporte(consulta);
            
            var factura = results.Where(x => x.NumeroFactura == consulta.NumeroFactura).FirstOrDefault();

            // Obtener los datos de la empresa (factura) con el IdEmpresa que llega en el request
            DatosEmpresa DatosEmpresaFactura = _SS.GetDatosEmpresa(Convert.ToInt32(consulta.IdEmpresa), Convert.ToInt32(consulta.IdSucursal));


            var data = new Facturas
            {
                DetalleFacturaList = detalleFactura,
                Factura = factura,
                DatosEmpresaFactura = DatosEmpresaFactura,
            };
            string baseUrl = $"{Request.Scheme}://{Request.Host.Value}"; // Obtener la URL base del servidor
            factura.logo = baseUrl + "/assets/" + consulta.empresa + ".png"; // armar la ruta completa para acceder al logo
            
            return new ViewAsPdf(data);
        }
      
        [HttpPost("FacturacionReporte/ReportFacturaMini")]// Reimprimir factura 8.5x11
        public IActionResult ReportFacturaMini(FacturaFilter consulta)
        {
            var detalleFactura = (List<DetalleFactura>)facturasNegocio.GetDetalleFacturaConsulta(consulta.NumeroFactura, consulta.Almacen);

            var results = (List<FacturaConsulta>)facturasNegocio.GetFacturasReporte(consulta);

            var factura = results.Where(x => x.NumeroFactura == consulta.NumeroFactura).FirstOrDefault();

            // Obtener los datos de la empresa (factura) con el IdEmpresa que llega en el request
            DatosEmpresa DatosEmpresaFactura = _SS.GetDatosEmpresa(Convert.ToInt32(consulta.IdEmpresa), Convert.ToInt32(consulta.IdSucursal));


            var data = new Facturas
            {
                DetalleFacturaList = detalleFactura,
                Factura = factura,
                DatosEmpresaFactura = DatosEmpresaFactura,
            };

            string baseUrl = $"{Request.Scheme}://{Request.Host.Value}"; // Obtener la URL base del servidor
            factura.logo = baseUrl + "/assets/" + consulta.empresa + ".png"; // armar la ruta completa para acceder al logo

            return new ViewAsPdf(data);
        }
        [HttpPost("FacturacionReporte/ReportCotizacion")]
        public IActionResult ReportCotizacion(CotizacionModel model)
        {
            string baseUrl = $"{Request.Scheme}://{Request.Host.Value}"; // Obtener la URL base del servidor
            model.Empresa = baseUrl + "/assets/" + model.Empresa + ".png"; // armar la ruta completa para acceder al logo
            return new ViewAsPdf(model);
        }

    }
}
