using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
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
        private readonly Clientes_Negocio _clientsNegocio;
        private readonly Catalogos_Negocio _catalogosNegocio;
        private readonly Facturas_Negocio _facturasNegocio;
        private readonly IAccesoDatos _accesoDatos;
        private readonly SeguridadService _seguridadService;
        private readonly FacturacionSettings _facturacionSettings;
        private readonly ILogger<FacturacionReporteController> _logger;

        public FacturacionReporteController(ILogger<FacturacionReporteController> logger, SeguridadService seguridadService,IAccesoDatos accesoDatos, Clientes_Negocio clientsNegocio, Catalogos_Negocio catalogosNegocio,Facturas_Negocio facturasNegocio, IOptions<FacturacionSettings> facturacionSettings)
        {
            _logger = logger;
            _accesoDatos = accesoDatos;
            _catalogosNegocio = catalogosNegocio;
            _clientsNegocio = clientsNegocio;
            _facturasNegocio = facturasNegocio;
            _seguridadService = seguridadService;
            _facturacionSettings = facturacionSettings.Value;
        }

        [HttpPost]
        [Route("FacturacionReporte/RptFactura")]// Generar Factura Nueva
        //[Authorize(Roles = "MNU_MANT_FACTURACION")]
        public IActionResult RptFactura(Facturas factura)
        {
            if (factura.Cliente != null)
                factura.IdCliente = _clientsNegocio.Cliente_Consulta_Por_Id_Cliente(factura.IdCliente).Id_Cliente;

            var result = _accesoDatos.PostFactura(factura);
            if (result == null)
                return NotFound();
          
            string baseUrl = $"{Request.Scheme}://{Request.Host.Value}"; // Obtener la URL base del servidor
            factura.logo = baseUrl + "/assets/" + factura.empresa + ".png"; // armar la ruta completa para acceder al logo

            factura.TipoPago = ((List<Tipos_Pagos>)_catalogosNegocio
                    .GetTipos_Pagos_Lista())
                    .Where(x => x.ID_Tipo_Pagos == factura.IdTipoPago)
                    .Select(p => p.Descripcion_Tipo_Pagos)
                    .FirstOrDefault();
            factura.TipoFactura = ((List<Tipos_Facturas>)_catalogosNegocio
                  .GetTipos_Factura_Lista())
                  .Where(x => x.ID_Tipo_Factura == factura.IdTipoFactura)
                  .Select(p => p.Descripcion_Factura_Tipo)
                  .FirstOrDefault();
            var facturaReporte = _accesoDatos.ObtenerFacturaReporte(result.NumeroFactura, 1);
            var cliente = _clientsNegocio.Cliente_Consulta_Por_Id_Cliente(factura.IdCliente);
            var vendedor = _catalogosNegocio.GetVendedor_Lista().Where(x => x.ID_Vendedor == factura.IdVendedor).Select(x => x.Nombres).FirstOrDefault();

            // Obtener los datos de la empresa (factura) con el IdEmpresa que llega en el request
            DatosEmpresa DatosEmpresaFactura = _seguridadService.GetDatosEmpresa(Convert.ToInt32(factura.IdEmpresa), Convert.ToInt32(factura.IdSucursal));

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

        [HttpPost("FacturacionReporte/ReportFacturaConsulta")]// Imprimir reporte de facturas para cierre de ventas
        public IActionResult ReportFacturaConsulta(FacturaFilter consulta)
        {
            consulta.PageIndex = 1;
            consulta.PageSize = 10000;

            var facturas = (List<FacturaConsulta>)_facturasNegocio.GetFacturas(consulta);
            DatosEmpresa DatosEmpresaFactura = _seguridadService.GetDatosEmpresa(Convert.ToInt32(consulta.IdEmpresa), Convert.ToInt32(consulta.IdSucursal));

            string baseUrl = $"{Request.Scheme}://{Request.Host.Value}";
           
            DatosEmpresaFactura.Logo = baseUrl + "/assets/" + DatosEmpresaFactura.Logo + ".png"; // armar la ruta completa para acceder al logo

            var data = new FacturaConsultaReporteViewModel
            {
                Facturas = facturas,
                DatosEmpresaFactura = DatosEmpresaFactura
            };

            return new ViewAsPdf(data);
        }

        [HttpPost("FacturacionReporte/ReportFactura")]// Reimprimir factura 8.5x11
        public async Task<IActionResult> ReportFactura(FacturaFilter consulta)
        {
            var detalleFactura = (List<DetalleFactura>)_facturasNegocio.GetDetalleFacturaConsulta(consulta.NumeroFactura, consulta.Almacen);
            
            var results = (List<FacturaConsulta>)_facturasNegocio.GetFacturasReporte(consulta);
            
            var factura = results.Where(x => x.NumeroFactura == consulta.NumeroFactura).FirstOrDefault();

            // Obtener los datos de la empresa (factura) con el IdEmpresa que llega en el request
            DatosEmpresa DatosEmpresaFactura = _seguridadService.GetDatosEmpresa(Convert.ToInt32(consulta.IdEmpresa), Convert.ToInt32(consulta.IdSucursal));

            string baseUrl = $"{Request.Scheme}://{Request.Host.Value}"; // Obtener la URL base del servidor
            DatosEmpresaFactura.Logo = baseUrl + "/assets/" + DatosEmpresaFactura.Logo + ".png"; // armar la ruta completa para acceder al logo

            // Obtener usuarios autorizados desde configuración
            var usuariosAutorizadosCotizacion = new HashSet<string>(
                _facturacionSettings.UsuariosAutorizadosCotizacion, 
                StringComparer.OrdinalIgnoreCase);

            // Determinar si debe mostrar como cotización o factura
            bool esCotizacion = DatosEmpresaFactura.IdEmpresa == 1 
                             && factura.TipoFactura == "Al Por Mayor"
                             && usuariosAutorizadosCotizacion.Contains(consulta.Usuario);

            // Solo modificar si es cotización (los valores por defecto ya están en la clase)
            if (esCotizacion)
            {
                factura.FacturaTitulo = "Cotización No.";
                factura.FacturaTituloTipo = "Cotización Tipo";
                factura.VerNCF = "";
            }

            var data = new Facturas
            {
                Factura = factura,
                DetalleFacturaList = detalleFactura,
                DatosEmpresaFactura = DatosEmpresaFactura,
            };

            // Obtener QR URL del log de factura electrónica
            try
            {
                var log = await _accesoDatos.GetLogFacturaElectronica(consulta.NumeroFactura, "A");
                if (log?.QR_Url != null) data.QrUrl = log.QR_Url;
            }
            catch { }

            return new ViewAsPdf(data);
        }
      
        [HttpPost("FacturacionReporte/ReportFacturaMini")]// Reimprimir factura 80mm
        public async Task<IActionResult> ReportFacturaMini(FacturaFilter consulta)
        {
            var detalleFactura = (List<DetalleFactura>)_facturasNegocio.GetDetalleFacturaConsulta(consulta.NumeroFactura, consulta.Almacen);

            var results = (List<FacturaConsulta>)_facturasNegocio.GetFacturasReporte(consulta);

            var factura = results.Where(x => x.NumeroFactura == consulta.NumeroFactura).FirstOrDefault();

            // Obtener los datos de la empresa (factura) con el IdEmpresa que llega en el request
            DatosEmpresa DatosEmpresaFactura = _seguridadService.GetDatosEmpresa(Convert.ToInt32(consulta.IdEmpresa), Convert.ToInt32(consulta.IdSucursal));


            var data = new Facturas
            {
                DetalleFacturaList = detalleFactura,
                Factura = factura,
                DatosEmpresaFactura = DatosEmpresaFactura,
            };

            // Obtener QR URL del log de factura electrónica
            try
            {
                var log = await _accesoDatos.GetLogFacturaElectronica(consulta.NumeroFactura, "A");
                if (log?.QR_Url != null) data.QrUrl = log.QR_Url;
            }
            catch { }

            string baseUrl = $"{Request.Scheme}://{Request.Host.Value}"; // Obtener la URL base del servidor
            factura.logo = baseUrl + "/assets/" +DatosEmpresaFactura.Logo + ".png"; // armar la ruta completa para acceder al logo

            return new ViewAsPdf(data);
        }
        
        [HttpPost("FacturacionReporte/ReportCotizacion")]
        public IActionResult ReportCotizacion(CotizacionModel model)
        {
            // Guardar cotización en BD
            try
            {
                int numeroCotizacion = _accesoDatos.GuardarCotizacion(model);
                model.NumeroFactura = numeroCotizacion;
                _logger.LogInformation("[Cotizacion] Guardada #{Num}", numeroCotizacion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Cotizacion] Error al guardar cotización en BD");
            }

            string baseUrl = $"{Request.Scheme}://{Request.Host.Value}";
            
            // Obtener datos de empresa desde la BD
            DatosEmpresa datosEmpresa = _seguridadService.GetDatosEmpresa(Convert.ToInt32(model.IdEmpresa), Convert.ToInt32(model.IdSucursal));
            
            // Armar URL completa del logo usando el nombre que viene de la BD
            datosEmpresa.Logo = baseUrl + "/assets/" + datosEmpresa.Logo + ".png";
            
            // Pasar datos de empresa dentro del modelo
            model.DatosEmpresaCotizacion = datosEmpresa;
            
            return new ViewAsPdf(model);
        }

        [HttpPost("FacturacionReporte/CierreCajaReport")]
        public IActionResult GetCierreCajaReport(FacturaFilter filtro)
        {
            try
            {
                var result = _facturasNegocio.GetCierreCajaReporte(filtro);
                if (result == null)
                    return NotFound();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
