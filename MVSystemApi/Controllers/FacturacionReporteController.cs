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
        private readonly Cotizacion_Negocio _cotizacionNegocio;
        private readonly IAccesoDatos _accesoDatos;
        private readonly SeguridadService _seguridadService;
        private readonly FacturacionSettings _facturacionSettings;
        private readonly ILogger<FacturacionReporteController> _logger;

        public FacturacionReporteController(ILogger<FacturacionReporteController> logger, SeguridadService seguridadService,IAccesoDatos accesoDatos, Clientes_Negocio clientsNegocio, Catalogos_Negocio catalogosNegocio,Facturas_Negocio facturasNegocio, Cotizacion_Negocio cotizacionNegocio, IOptions<FacturacionSettings> facturacionSettings)
        {
            _logger = logger;
            _accesoDatos = accesoDatos;
            _catalogosNegocio = catalogosNegocio;
            _clientsNegocio = clientsNegocio;
            _facturasNegocio = facturasNegocio;
            _cotizacionNegocio = cotizacionNegocio;
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

        /// <summary>
        /// Busca la factura a reimprimir: primero activa; si no existe, anulada, y en ese caso
        /// completa Anulada / motivo / usuario / fecha para que la vista lo muestre.
        /// </summary>
        private FacturaConsulta ObtenerFacturaParaReimpresion(FacturaFilter consulta)
        {
            var results = (List<FacturaConsulta>)_facturasNegocio.GetFacturasReporte(consulta);
            var factura = results.Where(x => x.NumeroFactura == consulta.NumeroFactura).FirstOrDefault();
            if (factura != null) return factura;

            var anuladas = (List<FacturaConsulta>)_facturasNegocio.GetFacturasReporte(consulta, "X");
            factura = anuladas.Where(x => x.NumeroFactura == consulta.NumeroFactura).FirstOrDefault();
            if (factura == null) return null;

            factura.Anulada = true;
            var anulacion = _accesoDatos.ObtenerAnulacionFactura(consulta.NumeroFactura, consulta.Almacen);
            if (anulacion != null)
            {
                factura.MotivoAnulacion = anulacion.Motivo;
                factura.UsuarioAnulacion = anulacion.Usuario;
                factura.FechaAnulacion = anulacion.Fecha;
            }
            return factura;
        }

        [HttpPost("FacturacionReporte/ReportFactura")]// Reimprimir factura 8.5x11
        public async Task<IActionResult> ReportFactura(FacturaFilter consulta)
        {
            var detalleFactura = (List<DetalleFactura>)_facturasNegocio.GetDetalleFacturaConsulta(consulta.NumeroFactura, consulta.Almacen);
            
            // Activa o, si no existe activa, anulada (con su motivo)
            var factura = ObtenerFacturaParaReimpresion(consulta);
            if (factura == null)
                return NotFound("No se encontró la factura.");

            // Una factura anulada no tiene renglones activos: se leen los anulados para el resumen por modelo
            if (factura.Anulada && detalleFactura.Count == 0)
                detalleFactura = _accesoDatos.ObtenerDetalleFacturaAnulada(consulta.NumeroFactura, consulta.Almacen);

            // Obtener los datos de la empresa (factura) con el IdEmpresa que llega en el request
            DatosEmpresa DatosEmpresaFactura = _seguridadService.GetDatosEmpresa(Convert.ToInt32(consulta.IdEmpresa), Convert.ToInt32(consulta.IdSucursal));

            string baseUrl = $"{Request.Scheme}://{Request.Host.Value}"; // Obtener la URL base del servidor
            DatosEmpresaFactura.Logo = baseUrl + "/assets/" + DatosEmpresaFactura.Logo + ".png"; // armar la ruta completa para acceder al logo

            // Obtener usuarios autorizados desde configuración
            var usuariosAutorizadosCotizacion = new HashSet<string>(
                _facturacionSettings.UsuariosAutorizadosCotizacion, 
                StringComparer.OrdinalIgnoreCase);

            // Determinar si debe mostrar como cotización o factura
            bool esCotizacion = DatosEmpresaFactura.IdEmpresa == 30
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
                if (log?.QR_Url != null)
                {
                    data.QrUrl = log.QR_Url;
                    // Extraer FechaFirma y CodigoSeguridad de la URL
                    var (fechaFirma, codigoSeguridad) = ExtractQrParameters(log.QR_Url);
                    data.FechaFirma = fechaFirma;
                    data.CodigoSeguridad = codigoSeguridad;
                }
            }
            catch { }

            return new ViewAsPdf(data);
        }
      
        [HttpPost("FacturacionReporte/ReportFacturaMini")]// Reimprimir factura 80mm
        public async Task<IActionResult> ReportFacturaMini(FacturaFilter consulta)
        {
            var detalleFactura = (List<DetalleFactura>)_facturasNegocio.GetDetalleFacturaConsulta(consulta.NumeroFactura, consulta.Almacen);

            // Activa o, si no existe activa, anulada (con su motivo)
            var factura = ObtenerFacturaParaReimpresion(consulta);
            if (factura == null)
                return NotFound("No se encontró la factura.");

            // Una factura anulada no tiene renglones activos: se leen los anulados para el resumen por modelo
            if (factura.Anulada && detalleFactura.Count == 0)
                detalleFactura = _accesoDatos.ObtenerDetalleFacturaAnulada(consulta.NumeroFactura, consulta.Almacen);

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
                if (log?.QR_Url != null)
                {
                    data.QrUrl = log.QR_Url;
                    // Extraer FechaFirma y CodigoSeguridad de la URL
                    var (fechaFirma, codigoSeguridad) = ExtractQrParameters(log.QR_Url);
                    data.FechaFirma = fechaFirma;
                    data.CodigoSeguridad = codigoSeguridad;
                }
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
                if (numeroCotizacion <= 0)
                {
                    _logger.LogError("[Cotizacion] Guardado sin número válido");
                    return StatusCode(500, "No fue posible guardar la cotización.");
                }

                model.NumeroFactura = numeroCotizacion;
                _logger.LogInformation("[Cotizacion] Guardada #{Num}", numeroCotizacion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Cotizacion] Error al guardar cotización en BD");
                return StatusCode(500, "Ocurrió un error al guardar la cotización.");
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

        [HttpGet("FacturacionReporte/ReportCotizacionConsulta/{codigo}/{idEmpresa}")]
        public IActionResult ReportCotizacionConsulta(int codigo, int idEmpresa)
        {
            try
            {
                var cotizacion = _cotizacionNegocio.GetCotizacionPorCodigo(codigo);
                if (cotizacion == null) return NotFound();

                cotizacion.IdEmpresa = idEmpresa.ToString();
                DatosEmpresa datosEmpresa = _seguridadService.GetDatosEmpresa(idEmpresa, Convert.ToInt32(cotizacion.IdSucursal));
                string baseUrl = $"{Request.Scheme}://{Request.Host.Value}";
                datosEmpresa.Logo = baseUrl + "/assets/" + datosEmpresa.Logo + ".png";
                cotizacion.DatosEmpresaCotizacion = datosEmpresa;

                return new ViewAsPdf("ReportCotizacion", cotizacion);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Cotizacion] Error al reimprimir cotización #{Codigo}", codigo);
                return StatusCode(500, "No fue posible reimprimir la cotización.");
            }
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

        /// <summary>
        /// Extrae parámetros de la URL del QR de validación de DGI
        /// Busca FechaFirma y CodigoSeguridad en los parámetros de la URL
        /// </summary>
        private (string FechaFirma, string CodigoSeguridad) ExtractQrParameters(string qrUrl)
        {
            if (string.IsNullOrEmpty(qrUrl))
                return (null, null);

            try
            {
                var uri = new Uri(qrUrl);
                var queryParams = System.Web.HttpUtility.ParseQueryString(uri.Query);
                
                string fechaFirma = queryParams["FechaFirma"];
                string codigoSeguridad = queryParams["CodigoSeguridad"];

                return (fechaFirma, codigoSeguridad);
            }
            catch
            {
                return (null, null);
            }
        }
    }
}
