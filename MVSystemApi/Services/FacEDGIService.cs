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
            // Literal del log = el literal real de la factura (antes "A" fijo: con varias sucursales el índice único
            // (número, literal) hacía chocar los logs de dos facturas con el mismo número). Si no se puede leer, "A" como antes.
            var literalLog = "A";
            try
            {
                var literalReal = await _accesoDatos.ObtenerLiteralFactura(factura.NumeroFactura, factura.IdAlmacen ?? 0);
                if (!string.IsNullOrWhiteSpace(literalReal)) literalLog = literalReal;
            }
            catch (Exception exLiteral)
            {
                _logger.LogWarning(exLiteral, "[LOG-LITERAL] No se pudo leer el literal de la factura {Factura}; se usa 'A'", factura.NumeroFactura);
            }

            try
            {
                var request = MapearFacturaARequest(factura, detalles, pagos, rncEmisor, nombreEmisor);
                var jsonOptions = new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.Never  // Incluir null values
                };
                var jsonContent = JsonSerializer.Serialize(request, jsonOptions);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var httpClient = _httpClientFactory.CreateClient("FacEDGI");
                var response = await httpClient.PostAsync(_invoicesEndpoint, content);

                var responseContent = await response.Content.ReadAsStringAsync();
                EasyConexionResponseDTO responseDto = null;
                var jsonDeserializeOptions = new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = JsonIgnoreCondition.Never
                };
                
                if (response.IsSuccessStatusCode)
                {
                    responseDto = JsonSerializer.Deserialize<EasyConexionResponseDTO>(responseContent, jsonDeserializeOptions);
                }
                else
                {
                    var errorMsg = $"HTTP {response.StatusCode}";
                    if (!string.IsNullOrEmpty(responseContent))
                        errorMsg += $": {responseContent}";
                    
                    responseDto = new EasyConexionResponseDTO
                    {
                        Success = false,
                        Message = errorMsg
                    };
                }

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
                    }
                    catch (Exception exQr)
                    {
                    }
                }

                // Estado real: EasyConexión responde al crear el documento (PEND) y la DGII contesta después. Se espera unos segundos
                // (FacEDGI:EsperaEstadoSegundos, por defecto 45): APRO = aprobada; cualquier otro valor distinto de PEND (p. ej. RECH) =
                // rechazada, y la factura se devuelve como error SIN reintentar (el controlador la anula); PEND al agotar la espera =
                // sigue pendiente y la factura queda como hasta ahora.
                var estadoLog = responseDto?.Success == true ? "ENVIADA" : "ERROR";
                var errorLog = responseDto?.Success == true ? null : responseDto?.Message;
                if (responseDto?.Success == true && responseDto.DocumentId > 0)
                {
                    var (estatus, motivo, qrEstado) = await EsperarEstadoFacturaAsync(httpClient, responseDto.DocumentId);
                    if (estatus == "APRO")
                    {
                        estadoLog = "ACEPTADA";
                        if (!string.IsNullOrWhiteSpace(qrEstado)) qrUrl = qrEstado;
                    }
                    else if (!string.IsNullOrEmpty(estatus) && estatus != "PEND")
                    {
                        estadoLog = "RECHAZADO_DGII";
                        errorLog = $"{estatus}: {motivo}".Trim();
                        responseDto.Success = false;
                        responseDto.Status = estatus;
                        responseDto.RechazadaPorDgii = true;
                        responseDto.Message = $"La DGII rechazó la factura ({estatus}). {motivo}".Trim();
                    }
                }

                var logFactura = new LogFacturaElectronica
                {
                    Numero_Factura = factura.NumeroFactura,
                    Literal = literalLog,
                    DocumentoId_EasyConexion = responseDto?.DocumentId,
                    VoucherNumber_DGII = responseDto?.VoucherNumber,
                    Estado = estadoLog,
                    Respuesta_JSON = responseContent,
                    Fecha_Envio = DateTime.Now,
                    Error_Descripcion = errorLog,
                    Usuario = usuario,
                    QR_Url = qrUrl
                };

                var logInsertado = await InsertLogFacturaElectronica(logFactura);
                return responseDto;
            }
            catch (Exception ex)
            {
                // Guardar error en log
                var logError = new LogFacturaElectronica
                {
                    Numero_Factura = factura.NumeroFactura,
                    Literal = literalLog,
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
            
            // Si la empresa asume el ITBIS (S) el precio ya lo incluye; si lo suma al cliente (N) el precio va sin ITBIS.
            var asumeItbis = EmpresaAsumeItbis();

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
                PaymentCondition = "1",  // Siempre "1" según DGI
                // S: monto con ITBIS incluido (indicador de la configuración, 1). N: montos sin ITBIS (0). Debe coincidir con priceIncludesTax de las líneas.
                TaxedAmountIndicator = asumeItbis ? _taxedAmountIndicator : 0,

                // Fechas
                IssueDate = factura.FechaRegistro,
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
            request.Lines = MapearDetalles(detalles, asumeItbis);

            // Mapear métodos de pago
            request.Payments = MapearPagos(pagos, factura.Total);

            // Log del total a enviar a DGI
            _logger.LogInformation($"[REQUEST DGI] Comprobante: {request.VoucherNumber}");
            _logger.LogInformation($"[REQUEST DGI] Tipo: {request.VoucherType} - Comprador: {request.BuyerName}");
            _logger.LogInformation($"[REQUEST DGI] Tipo de Pago Principal: ID={factura.IdTipoPago} → Código DGI={request.PaymentCondition}");
            _logger.LogInformation($"[REQUEST DGI] Total a enviar: RD${request.TotalAmount:F2}");
            _logger.LogInformation($"[REQUEST DGI] Líneas: {request.Lines.Count}, Métodos de pago: {request.Payments.Count}");
            
            // Log de detalle de métodos de pago
            if (request.Payments != null && request.Payments.Count > 0)
            {
                foreach (var i in Enumerable.Range(0, request.Payments.Count))
                {
                    _logger.LogInformation($"[REQUEST DGI] Pago[{i}]: Método={request.Payments[i].Method} (Código DGI), Monto=RD${request.Payments[i].Amount:F2}");
                }
            }

            return request;
        }

        /// <summary>
        /// ¿La empresa asume el ITBIS? Parámetro AsumeITBIS de la empresa ('S' = el precio ya lo incluye; 'N' = se suma al cliente).
        /// Si no se puede leer se usa 'S', que es como se enviaba siempre hasta ahora.
        /// </summary>
        private bool EmpresaAsumeItbis()
        {
            try
            {
                var dt = _accesoDatos.AsumeITBIS();
                if (dt != null && dt.Rows.Count > 0)
                    return string.Equals((Convert.ToString(dt.Rows[0]["AsumeITBIS"]) ?? "").Trim(), "S", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[ITBIS] No se pudo leer 'asume ITBIS' de la empresa; se usa 'S' (precio con ITBIS incluido)");
            }
            return true;
        }

        /// <summary>
        /// Mapea DetalleFactura[] a EasyConexionLineDTO[]
        /// Envía Amount = SubTotal + Descuento + ITBIS (total con impuesto incluido).
        /// TaxedAmountIndicator=1 indica a DGI que el Amount ya incluye ITBIS, por lo que lo extrae correctamente.
        /// </summary>
        private List<EasyConexionLineDTO> MapearDetalles(List<DetalleFactura> detalles, bool asumeItbis)
        {
            var lines = new List<EasyConexionLineDTO>();

            if (detalles == null || detalles.Count == 0)
                return lines;

            _logger.LogInformation($"[LÍNEAS] Mapeando {detalles.Count} línea(s)...");

            int lineNumber = 1;
            foreach (var detalle in detalles)
            {
                // Amount = SubTotal neto + Descuento + ITBIS (total con impuesto incluido)
                // TaxedAmountIndicator=1 indica a DGI que el Amount YA incluye ITBIS
                // Empresa que asume el ITBIS (S): el precio lo incluye -> Amount = SubTotal + Descuento + ITBIS.
                // Empresa que lo suma al cliente (N): el precio va sin ITBIS -> Amount = SubTotal + Descuento (el ITBIS se agrega aparte).
                var subtotalBruto = asumeItbis ? detalle.SubTotal + detalle.Descuento + detalle.Itbis : detalle.SubTotal + detalle.Descuento;

                // Descuento: regla compartida con el proveedor 2 (DescuentoComprobante): el monto va YA descontado y el descuento no se envía.
                var (montoLinea, precioUnitario) = DescuentoComprobante.AplicarEnLinea(subtotalBruto, detalle.Precio, detalle.Cantidad, detalle.Descuento);

                _logger.LogInformation($"[LÍNEA {lineNumber}] '{detalle.Descripcion}'");
                _logger.LogInformation($"   Precio unitario: RD${detalle.Precio:F2} × Qty: {detalle.Cantidad}");
                _logger.LogInformation($"   SubTotal: RD${detalle.SubTotal:F2}, Descuento: RD${detalle.Descuento:F2}, ITBIS: RD${detalle.Itbis:F2}");
                _logger.LogInformation($"   Amount a enviar (con ITBIS): RD${subtotalBruto:F2}");
                
                lines.Add(new EasyConexionLineDTO
                {
                    LineNumber = lineNumber++,
                    Description = detalle.Descripcion,
                    Quantity = detalle.Cantidad,
                    UnitPrice = precioUnitario,
                    Amount = montoLinea,
                    TaxCategory = 1,
                    Discount = null,   // el descuento ya va aplicado en el monto (DescuentoComprobante)
                    Surcharge = null,
                    GoodOrServiceIndicator = 1,
                    UnitMeasure = null,
                    ItbisRetAmount = null,
                    IsrRetAmount = null,
                    RetentionAgentInd = null,
                    PriceIncludesTax = asumeItbis
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
                _logger.LogInformation($"[MAPEO-PAGOS] ================================");
                _logger.LogInformation($"[MAPEO-PAGOS] Procesando {pagos.Count} métodos de pago");
                foreach (var pago in pagos)
                {
                    _logger.LogInformation($"[MAPEO-PAGOS] Procesando pago: ID_Tipo_Pago={pago.ID_Tipo_Pago}, Monto={pago.Monto_Pago}");
                    var codigoDGI = ObtenerCodigoDGI(pago.ID_Tipo_Pago);
                    _logger.LogInformation($"[MAPEO-PAGOS] Resultado: ID_Tipo_Pago={pago.ID_Tipo_Pago} → Código_DGI devuelto={codigoDGI}");
                    payments.Add(new EasyConexionPaymentDTO
                    {
                        Method = int.Parse(codigoDGI), // Convertir string "01" → int 1, etc.
                        Amount = pago.Monto_Pago
                    });
                }
                _logger.LogInformation($"[MAPEO-PAGOS] ================================");
            }
            else
            {
                // Si no hay detalles, asumir pago único con efectivo (método 1 = código DGI "01")
                _logger.LogWarning($"[MAPEO-PAGOS] ⚠ No hay detalles de pago. Asumiendo pago único con Efectivo (1)");
                payments.Add(new EasyConexionPaymentDTO
                {
                    Method = 1, // Efectivo por defecto (código DGI como int)
                    Amount = totalFactura
                });
            }

            return payments;
        }

        // ===================== Nota de crédito (proveedor 1 - EasyConexión) =====================

        private const int NcMaxIntentos = 30;     // 30 consultas x 2 s = 60 s
        private const int NcEsperaMs = 2000;

        /// <summary>
        /// Emite la nota de crédito total (E34) de una factura con e-NCF y espera la respuesta de la DGII hasta 60 s.
        /// Idempotente: si la factura ya tiene nota de crédito, no emite otra (consulta su estado y continúa desde ahí).
        /// El log de la NC usa el literal "N" + literal real de la factura. La anulación de la factura es la Parte 3.
        /// </summary>
        public async Task<NotaCreditoResultado> EmitirNotaCreditoAsync(int numeroFactura, int sucursal, string usuario, string rncEmisor, string nombreEmisor)
        {
            // Candado en la base: dos solicitudes simultáneas para la misma factura no se procesan a la vez
            await using var candado = await _accesoDatos.TomarCandadoAsync($"NC_{sucursal}_{numeroFactura}");
            if (candado == null)
                throw new NotaCreditoException("Esta factura ya se está procesando.");

            var origen = await _accesoDatos.ObtenerFacturaOrigenNotaCredito(numeroFactura, sucursal);
            if (origen == null)
                throw new NotaCreditoException("Factura no encontrada.");
            if (string.IsNullOrWhiteSpace(origen.Literal))
                throw new NotaCreditoException("La factura no tiene literal de sucursal; no se puede emitir la nota de crédito.");

            var literalNc = "N" + origen.Literal.Trim();
            var http = _httpClientFactory.CreateClient("FacEDGI");
            var logNc = await _accesoDatos.GetLogFacturaElectronica(numeroFactura, literalNc);

            // 1. Idempotencia: si ya hay NC no se emite otra, aunque la factura ya no esté activa
            if (logNc != null)
            {
                var estadoLog = (logNc.Estado ?? "").Trim().ToUpperInvariant();
                if (estadoLog == "ACEPTADA")
                    return await ResultadoAprobadaAsync(logNc.VoucherNumber_DGII, origen, sucursal, usuario, "La nota de crédito ya fue aprobada por la DGII.");
                if (estadoLog != "RECHAZADA" && estadoLog != "ERROR")
                    return await EsperarNotaCreditoAsync(http, logNc, origen, sucursal, usuario);   // PEND: sigue esperando la respuesta
                // RECHAZADA o ERROR: la anterior no prosperó; se emite una nueva sobre el mismo registro de log
            }

            // 2. Validaciones para emitir una NC nueva
            if (!string.Equals(origen.Estado, "A", StringComparison.OrdinalIgnoreCase))
                throw new NotaCreditoException("La factura no está activa; no se puede emitir una nota de crédito.");
            if (string.IsNullOrWhiteSpace(origen.NcfNumero) || !origen.NcfNumero.StartsWith("E", StringComparison.OrdinalIgnoreCase))
                throw new NotaCreditoException("La factura no tiene comprobante electrónico.");
            if (origen.Detalles.Count == 0)
                throw new NotaCreditoException("La factura no tiene líneas activas para incluir en la nota de crédito.");

            var logOrigen = await BuscarLogOrigenAsync(origen);
            if (logOrigen == null)
                throw new NotaCreditoException("No se encontró el comprobante electrónico de esta factura.");

            var (estatusOrigen, motivoOrigen, _) = await ConsultarEstatusAsync(http, logOrigen.DocumentoId_EasyConexion.Value);
            if (estatusOrigen == null)
                throw new NotaCreditoException("No se pudo consultar el estado del comprobante de la factura en el proveedor. Intente nuevamente.");
            if (estatusOrigen != "APRO")
            {
                // Rechazada por la DGII (estado final distinto de PEND): la factura nunca fue válida, no hace falta una nota de crédito.
                // Se anula directamente para que no quede activa una factura que la DGII no aceptó.
                if (estatusOrigen != "" && estatusOrigen != "PEND")
                {
                    logOrigen.Estado = "RECHAZADO_DGII";
                    logOrigen.Error_Descripcion = $"{estatusOrigen}: {motivoOrigen}".Trim();
                    await _accesoDatos.UpdateLogFacturaElectronica(logOrigen);

                    var (anuladaDirecta, errorAnulacion) = await _accesoDatos.AnularFacturaPorNotaCreditoAsync(
                        numeroFactura, sucursal, usuario, null, "Anulada automáticamente: la DGII rechazó la factura electrónica");
                    if (!anuladaDirecta)
                        throw new NotaCreditoException($"La DGII rechazó esta factura ({estatusOrigen}), pero no se pudo anularla: {errorAnulacion}");

                    origen.Estado = "X";
                    return ResultadoNc(origen.NcfNumero, "ANULADA", origen,
                        $"La DGII había rechazado esta factura ({estatusOrigen}). {motivoOrigen} No hace falta nota de crédito: la factura quedó anulada.".Trim());
                }
                throw new NotaCreditoException($"Solo se puede emitir una nota de crédito de facturas aprobadas por la DGII (estado actual: {(estatusOrigen == "" ? "sin respuesta" : estatusOrigen)}).");
            }

            // 3. e-NCF de la nota de crédito (se reserva justo antes de enviar)
            // Si el intento anterior falló al validar el pedido (HTTP 4xx: el documento no se creó), ese e-NCF no se usó: se reutiliza
            // en vez de gastar otro número de la secuencia.
            string ncf = null;
            if (logNc != null
                && string.Equals((logNc.Estado ?? "").Trim(), "ERROR", StringComparison.OrdinalIgnoreCase)
                && logNc.DocumentoId_EasyConexion == null
                && !string.IsNullOrWhiteSpace(logNc.VoucherNumber_DGII)
                && (logNc.Error_Descripcion ?? "").StartsWith("HTTP 4", StringComparison.OrdinalIgnoreCase)
                // Si el proveedor dijo que ese e-NCF YA estaba registrado, no sirve reutilizarlo: se toma uno nuevo
                && (logNc.Error_Descripcion ?? "").IndexOf("already been registered", StringComparison.OrdinalIgnoreCase) < 0)
                ncf = logNc.VoucherNumber_DGII.Trim();
            ncf ??= await _accesoDatos.ObtenerNcfNotaCredito(34);
            if (string.IsNullOrWhiteSpace(ncf))
                throw new NotaCreditoException("No se pudo asignar el e-NCF de la nota de crédito.");

            // 4. Pedido: las líneas y pagos de la factura de origen, con los datos propios de la NC
            var ahoraRD = DateTime.UtcNow.AddHours(-4);   // hora de República Dominicana (UTC-4)
            var facturaRef = new Facturas
            {
                NumeroFactura = numeroFactura,
                NcfTipoNumero = origen.NcfTipoNumero,   // define los datos del comprador (31 con RNC y nombre)
                NcfNumero = ncf,
                IdCliente = origen.IdCliente,
                RNC = origen.RNC,
                Total = origen.Total,
                IdTipoPago = origen.IdTipoPago,
                fechaLimitePago = origen.TiempoCredito ?? default,
                FechaRegistro = ahoraRD,
            };
            var request = MapearFacturaARequest(facturaRef, origen.Detalles, origen.Pagos, rncEmisor, nombreEmisor);
            request.VoucherType = "34";
            request.VoucherNumber = ncf;
            request.IssueDate = ahoraRD;
            request.ModifiedNcf = origen.NcfNumero;
            request.ModifDateNcf = origen.FechaRegistro.ToString("yyyy-MM-dd");
            request.ModifReasonId = 1;                      // anulación total
            // taxedAmountIndicator NO se cambia: queda el de la configuración (igual que la factura de origen, 1 = el monto ya incluye ITBIS).
            // Las líneas se envían con priceIncludesTax = true, y el proveedor rechaza (HTTP 400) un indicador 0 contra esas líneas:
            // "priceIncludesTax (true) contradicts taxedAmountIndicator (0)".
            request.CreditNoteIndicator = (ahoraRD.Date - origen.FechaRegistro.Date).TotalDays > 30 ? 1 : 0;   // 0 = hasta 30 días, 1 = más
            request.RncNcfModified = null;

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.Never
            };
            HttpResponseMessage response;
            string responseContent;
            for (var intentoNcf = 1; ; intentoNcf++)
            {
                request.VoucherNumber = ncf;
                var content = new StringContent(JsonSerializer.Serialize(request, jsonOptions), Encoding.UTF8, "application/json");
                response = await http.PostAsync(_invoicesEndpoint, content);
                responseContent = await response.Content.ReadAsStringAsync();

                // El proveedor ya tiene ese e-NCF (la secuencia local quedó atrás de la suya): se toma el siguiente y se reintenta, hasta 3 veces
                var yaRegistrado = !response.IsSuccessStatusCode
                    && responseContent.IndexOf("already been registered", StringComparison.OrdinalIgnoreCase) >= 0;
                if (!yaRegistrado || intentoNcf >= 3) break;

                _logger.LogWarning("[NC-NCF-REPETIDO] El e-NCF {Ncf} ya estaba registrado en el proveedor; se toma el siguiente de la secuencia", ncf);
                ncf = await _accesoDatos.ObtenerNcfNotaCredito(34);
                if (string.IsNullOrWhiteSpace(ncf))
                    throw new NotaCreditoException("No se pudo asignar el e-NCF de la nota de crédito.");
            }

            EasyConexionResponseDTO dto = null;
            string qrUrl = null;
            if (response.IsSuccessStatusCode)
            {
                try { dto = JsonSerializer.Deserialize<EasyConexionResponseDTO>(responseContent, jsonOptions); } catch { /* se trata como fallo abajo */ }
                qrUrl = LeerTextoJson(responseContent, "qrUrl", "qrData");
            }
            var creada = dto?.Success == true && dto.DocumentId > 0;
            var errorEnvio = creada ? null
                : (dto?.Message ?? $"HTTP {(int)response.StatusCode}" + (string.IsNullOrWhiteSpace(responseContent) ? "" : $": {responseContent}"));

            // 5. Log de la NC (nuevo, o el mismo registro si la anterior fue rechazada o dio error)
            var nuevoLog = new LogFacturaElectronica
            {
                Numero_Factura = numeroFactura,
                Literal = literalNc,
                DocumentoId_EasyConexion = creada ? dto.DocumentId : (int?)null,
                VoucherNumber_DGII = string.IsNullOrWhiteSpace(dto?.VoucherNumber) ? ncf : dto.VoucherNumber,
                Estado = creada ? "PEND" : "ERROR",
                Respuesta_JSON = responseContent,
                Fecha_Envio = DateTime.Now,
                Error_Descripcion = errorEnvio,
                Usuario = usuario,
                QR_Url = qrUrl,
            };
            if (logNc != null)
                await _accesoDatos.UpdateLogFacturaElectronica(nuevoLog);
            else
                await _accesoDatos.InsertLogFacturaElectronica(nuevoLog);

            if (!creada)
            {
                _logger.LogError("[NC-ENVIO-FALLO] Factura {Factura} (sucursal {Sucursal}), e-NCF {Ncf}: {Error}", numeroFactura, sucursal, ncf, errorEnvio);
                throw new NotaCreditoException($"El proveedor no aceptó la nota de crédito: {errorEnvio}");
            }

            // 6. Espera de la DGII
            return await EsperarNotaCreditoAsync(http, nuevoLog, origen, sucursal, usuario);
        }

        /// <summary>
        /// Con la NC aprobada anula la factura, solo si sigue activa (Proc_Anular_Factura no mira el estado de las líneas: anular dos
        /// veces devolvería el inventario dos veces). Si la anulación falla queda [NC-ANULA-FALLO] y, al volver a pulsar, la
        /// idempotencia reintenta solo la anulación.
        /// </summary>
        private async Task<NotaCreditoResultado> ResultadoAprobadaAsync(string ncf, FacturaOrigenNotaCredito origen, int sucursal, string usuario, string mensajeBase)
        {
            var (anulada, error) = await _accesoDatos.AnularFacturaPorNotaCreditoAsync(origen.NumeroFactura, sucursal, usuario, ncf);
            if (anulada) origen.Estado = "X";
            var mensaje = anulada
                ? $"{mensajeBase} La factura quedó anulada."
                : $"{mensajeBase} Pero no se pudo anular la factura: {error}. Vuelva a pulsar el botón para reintentar la anulación.";
            return ResultadoNc(ncf, "APROBADA", origen, mensaje);
        }

        /// <summary>
        /// Espera corta del estado real de una factura recién enviada (FacEDGI:EsperaEstadoSegundos, por defecto 45 s, consulta
        /// cada 2 s). Devuelve el último estado conocido (null si no se pudo consultar) al agotar el tiempo o al ser final.
        /// </summary>
        private async Task<(string Estatus, string Motivo, string Qr)> EsperarEstadoFacturaAsync(HttpClient http, int documentId)
        {
            var segundos = int.TryParse(_configuration["FacEDGI:EsperaEstadoSegundos"], out var s) ? s : 45;
            var intentos = Math.Max(1, segundos / 2);

            (string Estatus, string Motivo, string Qr) ultimo = (null, null, null);
            for (int intento = 1; intento <= intentos; intento++)
            {
                await Task.Delay(NcEsperaMs);
                ultimo = await ConsultarEstatusAsync(http, documentId);
                if (!string.IsNullOrEmpty(ultimo.Estatus) && ultimo.Estatus != "PEND")
                    break;   // final: APRO o rechazada
            }
            return ultimo;
        }

        /// <summary>Consulta cada 2 s hasta 60 s. APRO = aprobada; PEND = seguir; cualquier otro valor = no aprobada.</summary>
        private async Task<NotaCreditoResultado> EsperarNotaCreditoAsync(HttpClient http, LogFacturaElectronica logNc, FacturaOrigenNotaCredito origen, int sucursal, string usuario)
        {
            if (logNc.DocumentoId_EasyConexion == null)
                throw new NotaCreditoException("La nota de crédito registrada no tiene identificador del proveedor; contacte a soporte.");

            var ncf = logNc.VoucherNumber_DGII;
            for (int intento = 1; intento <= NcMaxIntentos; intento++)
            {
                var (estatus, mensaje, qr) = await ConsultarEstatusAsync(http, logNc.DocumentoId_EasyConexion.Value);

                if (estatus == "APRO")
                {
                    logNc.Estado = "ACEPTADA";
                    logNc.Fecha_Confirmacion = DateTime.Now;
                    logNc.Error_Descripcion = null;
                    if (!string.IsNullOrWhiteSpace(qr)) logNc.QR_Url = qr;
                    await _accesoDatos.UpdateLogFacturaElectronica(logNc);
                    return await ResultadoAprobadaAsync(ncf, origen, sucursal, usuario, "La DGII aprobó la nota de crédito.");
                }

                if (!string.IsNullOrEmpty(estatus) && estatus != "PEND")
                {
                    // Cualquier otro valor: no se anula nada; se informa tal cual y queda en el log
                    logNc.Estado = "RECHAZADA";
                    logNc.Error_Descripcion = $"{estatus}: {mensaje}";
                    await _accesoDatos.UpdateLogFacturaElectronica(logNc);
                    return ResultadoNc(ncf, "RECHAZADA", origen, $"La DGII no aprobó la nota de crédito ({estatus}). {mensaje}".Trim());
                }

                if (intento < NcMaxIntentos)
                    await Task.Delay(NcEsperaMs);
            }

            return ResultadoNc(ncf, "PENDIENTE", origen,
                "Nota de crédito enviada; pendiente de respuesta de la DGII. Espere un minuto y vuelva a pulsar el botón: no se emite otra nota de crédito, solo se consulta la respuesta.");
        }

        /// <summary>
        /// Log de la factura de origen: primero con el literal real; si no hay (facturas anteriores, guardadas con "A" fijo),
        /// con "A". Solo vale si el e-NCF del log coincide con el de la factura.
        /// </summary>
        private async Task<LogFacturaElectronica> BuscarLogOrigenAsync(FacturaOrigenNotaCredito origen)
        {
            foreach (var literal in new[] { origen.Literal.Trim(), "A" }.Distinct())
            {
                var log = await _accesoDatos.GetLogFacturaElectronica(origen.NumeroFactura, literal);
                if (log != null
                    && log.DocumentoId_EasyConexion != null
                    && string.Equals(log.VoucherNumber_DGII?.Trim(), origen.NcfNumero, StringComparison.OrdinalIgnoreCase))
                    return log;
            }
            return null;
        }

        /// <summary>Estado de un comprobante en EasyConexión (en mayúsculas), mensaje de la DGII y QR. Estatus null si la consulta falló.</summary>
        private async Task<(string Estatus, string Mensaje, string Qr)> ConsultarEstatusAsync(HttpClient http, int documentId)
        {
            try
            {
                var respuesta = await http.GetAsync($"{_invoicesEndpoint}/{documentId}");
                if (!respuesta.IsSuccessStatusCode)
                    return (null, $"HTTP {(int)respuesta.StatusCode}", null);

                var json = await respuesta.Content.ReadAsStringAsync();
                var estatus = LeerTextoJson(json, "estatus", "status")?.Trim().ToUpperInvariant() ?? "";
                // El motivo de la DGII viene en seguimientos[].acceptanceComment (destino 1 = DGII), no en un campo propio
                var mensaje = LeerTextoJson(json, "dgiiMessage") ?? LeerMotivoSeguimiento(json);
                return (estatus, mensaje, LeerTextoJson(json, "qrData", "qrUrl"));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[NC] No se pudo consultar el comprobante {DocumentId} en el proveedor", documentId);
                return (null, ex.Message, null);
            }
        }

        /// <summary>
        /// Motivo de la respuesta de la DGII: acceptanceComment del seguimiento con destinationId = 1 (DGII); si no hay, el último
        /// comentario no vacío de cualquier destino. Null si no hay.
        /// </summary>
        private static string LeerMotivoSeguimiento(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Object) return null;
                foreach (var propiedad in doc.RootElement.EnumerateObject())
                {
                    if (!string.Equals(propiedad.Name, "seguimientos", StringComparison.OrdinalIgnoreCase) || propiedad.Value.ValueKind != JsonValueKind.Array)
                        continue;

                    string deLaDgii = null, cualquiera = null;
                    foreach (var seguimiento in propiedad.Value.EnumerateArray())
                    {
                        if (seguimiento.ValueKind != JsonValueKind.Object) continue;
                        string comentario = null;
                        int? destino = null;
                        foreach (var campo in seguimiento.EnumerateObject())
                        {
                            if (string.Equals(campo.Name, "acceptanceComment", StringComparison.OrdinalIgnoreCase) && campo.Value.ValueKind == JsonValueKind.String)
                                comentario = campo.Value.GetString();
                            else if (string.Equals(campo.Name, "destinationId", StringComparison.OrdinalIgnoreCase) && campo.Value.ValueKind == JsonValueKind.Number)
                                destino = campo.Value.GetInt32();
                        }
                        if (string.IsNullOrWhiteSpace(comentario)) continue;
                        cualquiera = comentario;
                        if (destino == 1) deLaDgii = comentario;
                    }
                    return deLaDgii ?? cualquiera;
                }
            }
            catch (JsonException)
            {
            }
            return null;
        }

        private static string LeerTextoJson(string json, params string[] nombres)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind != JsonValueKind.Object) return null;
                foreach (var nombre in nombres)
                    foreach (var propiedad in doc.RootElement.EnumerateObject())
                        if (string.Equals(propiedad.Name, nombre, StringComparison.OrdinalIgnoreCase) && propiedad.Value.ValueKind == JsonValueKind.String)
                            return propiedad.Value.GetString();
            }
            catch (JsonException)
            {
            }
            return null;
        }

        private static NotaCreditoResultado ResultadoNc(string ncf, string estado, FacturaOrigenNotaCredito origen, string mensaje) =>
            new NotaCreditoResultado
            {
                Ncf = ncf,
                Estado = estado,
                Anulada = string.Equals(origen.Estado, "X", StringComparison.OrdinalIgnoreCase),
                Mensaje = mensaje,
            };

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
        /// Obtiene el código DGI para un tipo de pago desde la BD
        /// Consulta la columna Codigo_DGI de la tabla Tipo_Pagos
        /// Si no encuentra o si Codigo_DGI es nulo, devuelve "01" (Efectivo) como defecto
        /// </summary>
        private string ObtenerCodigoDGI(int idTipoPago)
        {
            try
            {
                _logger.LogInformation($"[MAPEO-TIPOPAGO] ================================");
                _logger.LogInformation($"[MAPEO-TIPOPAGO] Iniciando búsqueda de código DGI");
                _logger.LogInformation($"[MAPEO-TIPOPAGO] ID_Tipo_Pago recibido: {idTipoPago}");
                
                var tipoPago = _accesoDatos.Tipo_Pagos_GetById(idTipoPago);
                _logger.LogInformation($"[MAPEO-TIPOPAGO] Resultado de GetById: tipoPago={tipoPago}");
                
                if (tipoPago != null)
                {
                    _logger.LogInformation($"[MAPEO-TIPOPAGO] Objeto devuelto:");
                    _logger.LogInformation($"[MAPEO-TIPOPAGO]   - ID: {tipoPago.ID_Tipo_Pagos}");
                    _logger.LogInformation($"[MAPEO-TIPOPAGO]   - Descripción: {tipoPago.Descripcion_Tipo_Pagos}");
                    _logger.LogInformation($"[MAPEO-TIPOPAGO]   - Codigo_DGI: '{tipoPago.Codigo_DGI}' (Length={tipoPago.Codigo_DGI?.Length ?? 0})");
                    
                    if (!string.IsNullOrEmpty(tipoPago.Codigo_DGI))
                    {
                        _logger.LogInformation($"[MAPEO-TIPOPAGO] ✓ ÉXITO | Devolviendo: {tipoPago.Codigo_DGI}");
                        return tipoPago.Codigo_DGI;
                    }
                    else
                    {
                        _logger.LogWarning($"[MAPEO-TIPOPAGO] ⚠ Codigo_DGI VACÍO o NULL. Devolviendo defecto: 01");
                        return "01";
                    }
                }
                else
                {
                    _logger.LogWarning($"[MAPEO-TIPOPAGO] ⚠ ObjETipoPago es NULL. Devolviendo defecto: 01");
                    return "01";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[MAPEO-TIPOPAGO] ✗ EXCEPCIÓN: {ex.GetType().Name}: {ex.Message}");
                _logger.LogError($"[MAPEO-TIPOPAGO] Stack trace: {ex.StackTrace}");
                return "01";
            }
            finally
            {
                _logger.LogInformation($"[MAPEO-TIPOPAGO] ================================\n");
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
