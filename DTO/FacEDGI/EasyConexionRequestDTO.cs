namespace DTO.FacEDGI
{
    /// <summary>
    /// DTO para el request enviado a DGI a través de EasyConexión API
    /// Mapea todos los datos de una factura MVSystem al formato requerido por DGI
    /// </summary>
    public class EasyConexionRequestDTO
    {
        /// <summary>
        /// Tipo de comprobante
        /// Origen: Tipo de comprobante (dropdown)
        /// Valores: "32" (Consumidor Final), "31" (Crédito Fiscal), etc.
        /// </summary>
        public string VoucherType { get; set; }

        /// <summary>
        /// Número de comprobante generado
        /// Origen: Comprobante generado por el sistema (E32...)
        /// Formato: "E32" + número secuencial (ej: "E320000000001")
        /// </summary>
        public string VoucherNumber { get; set; }

        /// <summary>
        /// RNC de la empresa que emite
        /// Origen: Token del usuario (datos de la empresa)
        /// </summary>
        public string IssuerRnc { get; set; }

        /// <summary>
        /// Nombre de la empresa que emite
        /// Origen: Token del usuario (datos de la empresa)
        /// </summary>
        public string IssuerName { get; set; }

        /// <summary>
        /// Dirección del emisor
        /// Origen: Empresas.Direccion o null
        /// </summary>
        public string IssuerAddress { get; set; }

        /// <summary>
        /// RNC del comprador
        /// Origen: Clientes.RNC (null si es consumidor final)
        /// </summary>
        public string BuyerRnc { get; set; }

        /// <summary>
        /// Nombre del comprador
        /// Origen: Clientes.Nombre o "Consumidor Final" si no hay RNC
        /// </summary>
        public string BuyerName { get; set; }

        /// <summary>
        /// Dirección del comprador
        /// Origen: Clientes.Direccion o null
        /// </summary>
        public string BuyerAddress { get; set; }

        /// <summary>
        /// Teléfono del comprador
        /// Origen: Clientes.Telefono o null
        /// </summary>
        public string BuyerPhone { get; set; }

        /// <summary>
        /// Identificación extranjera del comprador
        /// Origen: null por defecto
        /// </summary>
        public string ForeignId { get; set; }

        /// <summary>
        /// Email del comprador
        /// Origen: Clientes.Email o null
        /// </summary>
        public string BuyerEmail { get; set; }

        /// <summary>
        /// Contacto principal del comprador
        /// Origen: null por defecto
        /// </summary>
        public string BuyerContact { get; set; }

        /// <summary>
        /// Número de orden de compra
        /// Origen: null por defecto
        /// </summary>
        public string PurchaseOrderNumber { get; set; }

        /// <summary>
        /// Fecha de orden de compra
        /// Origen: null por defecto
        /// </summary>
        public DateTime? PurchaseOrderDate { get; set; }

        /// <summary>
        /// Tipo de moneda
        /// Valor: "DOP" (fijo)
        /// </summary>
        public string CurrencyType { get; set; } = "DOP";

        /// <summary>
        /// Tipo de cambio (si aplica)
        /// Origen: null por defecto (DOP es moneda local)
        /// </summary>
        public decimal? ExchangeRate { get; set; }

        /// <summary>
        /// Condición de pago
        /// Origen: Tipo_Pagos.Codigo_DGI
        /// Valores: "1" (Efectivo), "2" (Transferencia), "3" (Tarjeta), "4" (Crédito), etc. (SIN cero a la izquierda)
        /// </summary>
        public string PaymentCondition { get; set; }

        /// <summary>
        /// Tipo de ingreso
        /// Valor: "01" (ingreso ordinario - fijo en config)
        /// </summary>
        public string IncomeType { get; set; } = "01";

        /// <summary>
        /// Fecha de expiración de secuencia
        /// Origen: null por defecto
        /// </summary>
        public DateTime? SequenceExpDate { get; set; }
        public DateTime? IssueDate { get; set; }

        

        /// <summary>
        /// Fecha límite de pago
        /// Origen: Facturas.FechaLimitePago o null
        /// </summary>
        public DateTime? PaymentDeadline { get; set; }

        /// <summary>
        /// Indicador de monto gravable (con ITBIS)
        /// Valor: 1 (fijo - todos los montos incluyen ITBIS)
        /// </summary>
        public int TaxedAmountIndicator { get; set; } = 1;

        /// <summary>
        /// Monto total de la factura
        /// Origen: Facturas.Total
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Código de sucursal
        /// Origen: null por defecto (o Literal de sucursal si aplica)
        /// </summary>
        public string BranchCode { get; set; }

        /// <summary>
        /// Número de factura interna
        /// Origen: null por defecto (generado por DGI)
        /// </summary>
        public string InternalInvoiceNumber { get; set; }

        /// <summary>
        /// Código del vendedor
        /// Origen: Vendedores.Codigo o null
        /// </summary>
        public string SellerCode { get; set; }

        /// <summary>
        /// Fecha inicial del período (para facturas periódicas)
        /// Origen: null por defecto
        /// </summary>
        public DateTime? PeriodFrom { get; set; }

        /// <summary>
        /// Fecha final del período (para facturas periódicas)
        /// Origen: null por defecto
        /// </summary>
        public DateTime? PeriodTo { get; set; }

        /// <summary>
        /// Lista de líneas de detalle de la factura
        /// Origen: DetalleFactura[]
        /// Una línea por artículo vendido
        /// </summary>
        public List<EasyConexionLineDTO> Lines { get; set; } = new List<EasyConexionLineDTO>();

        /// <summary>
        /// Lista de métodos de pago utilizados
        /// Origen: DetallesPagos_Factura[]
        /// Una línea por método de pago, o un solo elemento si es pago único
        /// </summary>
        public List<EasyConexionPaymentDTO> Payments { get; set; } = new List<EasyConexionPaymentDTO>();

        // === Campos exclusivos para Notas de Crédito (tipo 34) ===

        /// <summary>
        /// NCF de la factura original que se está creditando
        /// Ejemplo: "E320000000104"
        /// </summary>
        public string ModifiedNcf { get; set; }

        /// <summary>
        /// RNC del emisor de la factura original
        /// Ejemplo: "132307382"
        /// </summary>
        public string RncNcfModified { get; set; }

        /// <summary>
        /// Fecha de la factura original que se modifica
        /// Formato: "yyyy-MM-dd"
        /// </summary>
        public string ModifDateNcf { get; set; }

        /// <summary>
        /// Motivo de la nota de crédito
        /// 1 = Devolución, 2 = Error en facturación, etc.
        /// </summary>
        public int? ModifReasonId { get; set; }

        /// <summary>
        /// Indicador de nota de crédito (1 = sí)
        /// </summary>
        public int? CreditNoteIndicator { get; set; }
    }
}
