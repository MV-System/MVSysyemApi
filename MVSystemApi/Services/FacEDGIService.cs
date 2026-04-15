using DTO.FacEDGI;
using MVSystemApi.Interfaz;
using MVSystemApi.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace MVSystemApi.Services
{
    /// <summary>
    /// Servicio de Facturación Electrónica (FacEDGI)
    /// Implementa la integración con EasyConexión API para envío de facturas a DGI
    /// </summary>
    public class FacEDGIService : IFacEDGIService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IAccesoDatos _accesoDatos;
        private readonly ILogger<FacEDGIService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// Configuración desde appsettings y contexto del usuario
        /// </summary>
        private string _apiPath => _configuration["FacEDGI:BaseUrl"];
        private string _invoicesEndpoint => $"{_apiPath}/invoices";
        
        /// <summary>
        /// RNC del emisor - Extraído del token JWT del usuario logueado
        /// Formateado como: XXX-XXXXX-X
        /// </summary>
        private string _issuerRnc
        {
            get
            {
                var rncClaim = _httpContextAccessor?.HttpContext?.User?.FindFirst("Rnc");
                var rnc = rncClaim?.Value ?? throw new InvalidOperationException("RNC no encontrado en el token JWT");
                return FormatearRnc(rnc);
            }
        }

        /// <summary>
        /// Nombre del emisor - Extraído del token JWT del usuario logueado
        /// </summary>
        private string _issuerName
        {
            get
            {
                var empresaClaim = _httpContextAccessor?.HttpContext?.User?.FindFirst("Empresa");
                return empresaClaim?.Value ?? throw new InvalidOperationException("Nombre de empresa no encontrado en el token JWT");
            }
        }

        private string _currencyType => _configuration["FacEDGI:CurrencyType"];
        private string _incomeType => _configuration["FacEDGI:IncomeType"];
        private int _taxedAmountIndicator => int.Parse(_configuration["FacEDGI:TaxedAmountIndicator"] ?? "1");

        public FacEDGIService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IAccesoDatos accesoDatos,
            ILogger<FacEDGIService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _accesoDatos = accesoDatos ?? throw new ArgumentNullException(nameof(accesoDatos));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        /// <summary>
        /// Envía una factura a la DGI a través de EasyConexión API
        /// </summary>
        public async Task<EasyConexionResponseDTO> EnviarFacturaADGII(
            Facturas factura,
            List<DetalleFactura> detalles,
            List<DetallesPagos_Factura> pagos,
            string usuario,
            string rncEmisor,
            string nombreEmisor)
        {
            try
            {
                var tipoComprobanteDesc = factura.NcfTipoNumero == 31 ? "CRÉDITO FISCAL (31)" : "CONSUMIDOR FINAL (32)";
                _logger.LogInformation($"[ENVÍO DGI] Factura {factura.NumeroFactura} | Tipo: {tipoComprobanteDesc} | Total: RD${factura.Total}");

                // 1. Mapear datos a RequestDTO (pasando RNC y empresa capturados del token)
                var request = MapearFacturaARequest(factura, detalles, pagos, rncEmisor, nombreEmisor);

                _logger.LogInformation($"[MAPEO OK] Comprador: {request.BuyerName} (RNC: {request.BuyerRnc}) | Líneas: {request.Lines?.Count ?? 0} | Monto: RD${request.TotalAmount}");

                // 2. Serializar a JSON con camelCase (incluir null values)
                var jsonOptions = new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.Never  // Incluir null values
                };
                var jsonContent = JsonSerializer.Serialize(request, jsonOptions);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // 3. Crear cliente HTTP para este envío (evita disposed object en background tasks)
                var httpClient = _httpClientFactory.CreateClient("FacEDGI");
                
                // 4. Enviar POST a EasyConexión API
                _logger.LogInformation($"[ENVIANDO] POST a {_invoicesEndpoint}");
                _logger.LogInformation($"[REQUEST JSON] {jsonContent}");
                var response = await httpClient.PostAsync(_invoicesEndpoint, content);

                // 5. Leer respuesta
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"[RESPUESTA] Status: {(int)response.StatusCode} {response.StatusCode}");
               
                // 6. Deserializar respuesta
                EasyConexionResponseDTO responseDto = null;
                var jsonDeserializeOptions = new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.Never
                };
                
                if (response.IsSuccessStatusCode)
                {
                    responseDto = JsonSerializer.Deserialize<EasyConexionResponseDTO>(responseContent, jsonDeserializeOptions);
                    _logger.LogInformation($"[ÉXITO] Factura {factura.NumeroFactura} enviada exitosamente | DocId: {responseDto?.DocumentId} | Voucher: {responseDto?.VoucherNumber}");
                }
                else
                {
                    _logger.LogError($"[ERROR HTTP] Status {(int)response.StatusCode} al enviar factura {factura.NumeroFactura}");
                    var errorMsg = $"HTTP {response.StatusCode}";
                    if (!string.IsNullOrEmpty(responseContent))
                        errorMsg += $": {responseContent}";
                    
                    responseDto = new EasyConexionResponseDTO
                    {
                        Success = false,
                        Message = errorMsg
                    };
                }

                // 7. Obtener QR URL de DGI consultando la factura creada
                string qrUrl = null;
                if (responseDto?.Success == true && responseDto.DocumentId > 0)
                {
                    try
                    {
                        var getResponse = await httpClient.GetAsync($"{_invoicesEndpoint}/{responseDto.DocumentId}");
                        if (getResponse.IsSuccessStatusCode)
                        {
                            var getContent = await getResponse.Content.ReadAsStringAsync();
                            using var doc = System.Text.Json.JsonDocument.Parse(getContent);
                            if (doc.RootElement.TryGetProperty("qrData", out var qrProp))
                                qrUrl = qrProp.GetString();
                        }
                        _logger.LogInformation($"[QR] Factura {factura.NumeroFactura} | QR URL: {qrUrl ?? "no disponible"}");
                    }
                    catch (Exception exQr)
                    {
                        _logger.LogWarning($"[QR] No se pudo obtener QR para factura {factura.NumeroFactura}: {exQr.Message}");
                    }
                }

                // 8. Guardar log en BD
                var logFactura = new LogFacturaElectronica
                {
                    Numero_Factura = factura.NumeroFactura,
                    Literal = "A", // Literal de sucursal por defecto
                    DocumentoId_EasyConexion = responseDto?.DocumentId,
                    VoucherNumber_DGII = responseDto?.VoucherNumber,
                    Estado = responseDto?.Success == true ? "ENVIADA" : "ERROR",
                    Respuesta_JSON = responseContent,
                    Fecha_Envio = DateTime.Now,
                    Error_Descripcion = responseDto?.Success == true ? null : responseDto?.Message,
                    Usuario = usuario,
                    QR_Url = qrUrl
                };

                var logInsertado = await InsertLogFacturaElectronica(logFactura);
                if (!logInsertado)
                {
                    _logger.LogWarning($"No se pudo guardar el log de la factura {factura.NumeroFactura} en BD");
                }

                return responseDto;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[EXCEPCIÓN] Error al enviar factura {factura.NumeroFactura}: {ex.Message}");
                _logger.LogError($"[STACK TRACE] {ex.StackTrace}");
                
                // Guardar error en log
                var logError = new LogFacturaElectronica
                {
                    Numero_Factura = factura.NumeroFactura,
                    Literal = "A",
                    Estado = "ERROR",
                    Respuesta_JSON = ex.Message,
                    Fecha_Envio = DateTime.Now,
                    Error_Descripcion = ex.Message,
                    Usuario = usuario
                };

                await InsertLogFacturaElectronica(logError);

                return new EasyConexionResponseDTO
                {
                    Success = false,
                    Message = $"Excepción: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Mapea una Factura + Detalles + Pagos al formato esperado por DGI
        /// </summary>
        private EasyConexionRequestDTO MapearFacturaARequest(
            Facturas factura,
            List<DetalleFactura> detalles,
            List<DetallesPagos_Factura> pagos,
            string rncEmisor,
            string nombreEmisor)
        {
            // Log de inicio del mapeo
            var tipoComprobanteDesc = factura.NcfTipoNumero == 31 ? "CRÉDITO FISCAL" : 
                                      factura.NcfTipoNumero == 32 ? "CONSUMIDOR FINAL" : "OTRO";
            _logger.LogInformation($"[MAPEO] Tipo Comprobante: {factura.NcfTipoNumero} ({tipoComprobanteDesc}), Número: {factura.NcfNumero}");
            _logger.LogInformation($"[MAPEO] Emisor: {nombreEmisor} (RNC: {rncEmisor})");
            
            // Si es consumidor final (32), usar "Consumidor Final", sino obtener nombre del cliente (31 = crédito fiscal)
            string buyerName = "Consumidor Final";
            if (factura.NcfTipoNumero == 31) // Es Crédito Fiscal, no consumidor final
            {
                if (factura.Cliente != null && !string.IsNullOrEmpty(factura.Cliente.Nombres))
                {
                    buyerName = factura.Cliente.Nombres;
                }
                else
                {
                    // Cliente no está populado, intentar obtenerlo de la BD
                    try
                    {
                        var clientes = _accesoDatos.Cliente_Consulta_Por_Id_Cliente(factura.IdCliente);
                        if (clientes != null && clientes.Rows.Count > 0)
                        {
                            var nombres = clientes.Rows[0]["Nombres"]?.ToString();
                            var apellidos = clientes.Rows[0]["Apellidos"]?.ToString();
                            buyerName = !string.IsNullOrEmpty(nombres) ? nombres : "Cliente";
                            if (!string.IsNullOrEmpty(apellidos))
                                buyerName += " " + apellidos;
                            _logger.LogInformation($"[MAPEO-CF] Cliente obtenido de BD: {buyerName}");
                        }
                        else
                        {
                            buyerName = "Cliente";
                            _logger.LogWarning($"[MAPEO-CF] Cliente no encontrado en BD, usando nombre genérico");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"[MAPEO-CF] Error obteniendo cliente de BD: {ex.Message}");
                        buyerName = "Cliente";
                    }
                }
            }
            
            var request = new EasyConexionRequestDTO
            {
                // Tipo de comprobante - Obtenido del modelo
                VoucherType = factura.NcfTipoNumero.ToString(), // E.g., "32" para consumidor final
                
                // Número de comprobante - Ya viene generado en la factura
                VoucherNumber = factura.NcfNumero, // E.g., "E320000000002"
                
                // Datos del emisor (desde config)
                IssuerRnc = !string.IsNullOrEmpty(rncEmisor) ? FormatearRnc(rncEmisor) : "", // Formatear RNC del emisor si no está vacío
                IssuerName = nombreEmisor,
                IssuerAddress = null,  // N/A para este sistema
                
                // Datos del comprador
                // Solo enviar RNC si ES Crédito Fiscal (NcfTipoNumero == 31)
                // Tipo 32 = CONSUMIDOR FINAL (e-CF) - no requiere RNC del cliente
                // Tipo 31 = CRÉDITO FISCAL (e-CF) - requiere RNC del cliente
                BuyerRnc = factura.NcfTipoNumero == 31 && !string.IsNullOrEmpty(factura.RNC) ? FormatearRnc(factura.RNC) : null,
                BuyerName = buyerName,
                BuyerAddress = null,
                BuyerPhone = null,
                ForeignId = null,
                BuyerEmail = null,
                BuyerContact = null,
                
                // Datos de orden de compra
                PurchaseOrderNumber = null,
                PurchaseOrderDate = null,
                
                // Configuración global
                CurrencyType = _currencyType,
                ExchangeRate = null,  // DOP es moneda local
                IncomeType = _incomeType,
                PaymentCondition = factura.IdTipoPago.ToString(), // Condición de pago
                TaxedAmountIndicator = _taxedAmountIndicator,
                
                // Fechas
                SequenceExpDate = null,
                PaymentDeadline = factura.IdTipoPago == 2 ? factura.fechaLimitePago : (DateTime?)null,  // Solo si es CRÉDITO (IdTipoPago = 2)
                
                // Totales
                TotalAmount = factura.Total,
                
                // Datos de sucursal y vendedor
                BranchCode = null,
                InternalInvoiceNumber = null,
                SellerCode = null,  // Vendedor podría mapearse desde factura.IdVendedor
                
                // Períodos
                PeriodFrom = null,
                PeriodTo = null
            };

            // Mapear líneas de detalle
            request.Lines = MapearDetalles(detalles);

            // Mapear métodos de pago
            request.Payments = MapearPagos(pagos, factura.Total);

            // Log del total a enviar a DGI
            _logger.LogInformation($"[REQUEST DGI] Comprobante: {request.VoucherNumber}");
            _logger.LogInformation($"[REQUEST DGI] Tipo: {request.VoucherType} - Comprador: {request.BuyerName}");
            _logger.LogInformation($"[REQUEST DGI] Total a enviar: RD${request.TotalAmount:F2}");
            _logger.LogInformation($"[REQUEST DGI] Líneas: {request.Lines.Count}, Métodos de pago: {request.Payments.Count}");

            return request;
        }

        /// <summary>
        /// Mapea DetalleFactura[] a EasyConexionLineDTO[]
        /// Envía Amount como SubTotal bruto (sin descuento) para que DGI aplique el descuento de su lado.
        /// Nuestro cálculo interno no cambia.
        /// </summary>
        private List<EasyConexionLineDTO> MapearDetalles(List<DetalleFactura> detalles)
        {
            var lines = new List<EasyConexionLineDTO>();

            if (detalles == null || detalles.Count == 0)
                return lines;

            _logger.LogInformation($"[LÍNEAS] Mapeando {detalles.Count} línea(s)...");

            int lineNumber = 1;
            foreach (var detalle in detalles)
            {
                // SubTotal bruto = SubTotal neto + Descuento (para que DGI lo reste)
                var subtotalBruto = detalle.SubTotal + detalle.Descuento;

                _logger.LogInformation($"[LÍNEA {lineNumber}] '{detalle.Descripcion}'");
                _logger.LogInformation($"   Precio unitario: RD${detalle.Precio:F2} × Qty: {detalle.Cantidad}");
                _logger.LogInformation($"   Descuento: RD${detalle.Descuento:F2}");
                _logger.LogInformation($"   Amount a enviar (bruto): RD${subtotalBruto:F2}");
                
                lines.Add(new EasyConexionLineDTO
                {
                    LineNumber = lineNumber++,
                    Description = detalle.Descripcion,
                    Quantity = detalle.Cantidad,
                    UnitPrice = detalle.Precio,
                    Amount = subtotalBruto,  // Bruto: DGI aplica el descuento
                    TaxCategory = 1,
                    Discount = detalle.Descuento != 0 ? detalle.Descuento : (decimal?)null,
                    Surcharge = null,
                    GoodOrServiceIndicator = 1,
                    UnitMeasure = null,
                    ItbisRetAmount = null,
                    IsrRetAmount = null,
                    RetentionAgentInd = null
                });
            }

            _logger.LogInformation($"[LÍNEAS] Total mapeado: {lines.Count} línea(s)");
            return lines;
        }

        /// <summary>
        /// Mapea DetallesPagos_Factura[] a EasyConexionPaymentDTO[]
        /// Si no hay detalles de pago, se crea uno con el total
        /// </summary>
        private List<EasyConexionPaymentDTO> MapearPagos(
            List<DetallesPagos_Factura> pagos,
            decimal totalFactura)
        {
            var payments = new List<EasyConexionPaymentDTO>();

            // Si hay detalles de pago registrados, usarlos
            if (pagos != null && pagos.Count > 0)
            {
                foreach (var pago in pagos)
                {
                    payments.Add(new EasyConexionPaymentDTO
                    {
                        Method = pago.ID_Tipo_Pago,
                        Amount = pago.Monto_Pago
                    });
                }
            }
            else
            {
                // Si no hay detalles, asumir pago único con efectivo (método 1)
                payments.Add(new EasyConexionPaymentDTO
                {
                    Method = 1, // Efectivo por defecto
                    Amount = totalFactura
                });
            }

            return payments;
        }

        /// <summary>
        /// Obtiene el log de envío de una factura específica
        /// </summary>
        public async Task<LogFacturaElectronica> GetLogFacturaElectronica(int numeroFactura, string literal)
        {
            try
            {
                _logger.LogInformation($"Obteniendo log de factura {numeroFactura}, literal {literal}");
                return await _accesoDatos.GetLogFacturaElectronica(numeroFactura, literal);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al obtener log de factura: {ex.Message}", ex);
                return null;
            }
        }

        /// <summary>
        /// Obtiene todas las facturas con estado PEND
        /// </summary>
        public async Task<List<LogFacturaElectronica>> GetLogFacturasElectronicasPendientes()
        {
            try
            {
                _logger.LogInformation("Obteniendo logs pendientes de facturas electrónicas");
                return await _accesoDatos.GetLogFacturasElectronicasPendientes();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al obtener logs pendientes: {ex.Message}", ex);
                return new List<LogFacturaElectronica>();
            }
        }

        /// <summary>
        /// Actualiza el estado de un log de factura electrónica
        /// </summary>
        public async Task<bool> UpdateLogFacturaElectronica(LogFacturaElectronica log)
        {
            try
            {
                _logger.LogInformation($"Actualizando log de factura {log.Numero_Factura}");
                return await _accesoDatos.UpdateLogFacturaElectronica(log);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al actualizar log de factura: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Inserta un nuevo registro de log para una factura
        /// </summary>
        public async Task<bool> InsertLogFacturaElectronica(LogFacturaElectronica log)
        {
            try
            {
                _logger.LogInformation($"Insertando log de factura {log.Numero_Factura}");
                return await _accesoDatos.InsertLogFacturaElectronica(log);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al insertar log de factura: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Formatea el RNC al formato esperado por DGI: XXX-XXXXX-X
        /// Ejemplo: "1323073820" → "132-30738-2" / "132307382" → "132-30738-2"
        /// </summary>
        private string FormatearRnc(string rnc)
        {
            try
            {
                // Remover guiones si existen
                rnc = rnc?.Replace("-", "").Trim();

                if (string.IsNullOrEmpty(rnc) || rnc.Length < 9)
                    throw new InvalidOperationException($"RNC inválido: {rnc}. Debe tener al menos 9 dígitos.");

                // Tomar últimos 9 dígitos si hubiera 10 (some systems provide 10-digit RNC)
                if (rnc.Length > 9)
                    rnc = rnc.Substring(rnc.Length - 9);

                // Formatear como XXX-XXXXX-X
                var rncFormateado = $"{rnc.Substring(0, 3)}-{rnc.Substring(3, 5)}-{rnc.Substring(8, 1)}";
                _logger.LogDebug($"RNC formateado: {rnc} → {rncFormateado}");

                return rncFormateado;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error al formatear RNC '{rnc}': {ex.Message}");
                throw;
            }
        }
    }
}
