namespace DTO.FacEDGI
{
    /// <summary>
    /// DTO para cada línea de detalle de factura enviada a DGI
    /// </summary>
    public class EasyConexionLineDTO
    {
        /// <summary>
        /// Número de línea secuencial (1, 2, 3...)
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Descripción del producto/servicio
        /// Origen: DetalleFactura.Descripcion
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Cantidad vendida
        /// Origen: DetalleFactura.Cantidad
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Precio unitario
        /// Origen: DetalleFactura.Precio
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Subtotal de la línea (sin ITBIS)
        /// Origen: DetalleFactura.SubTotal
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Indicador de categoría de impuesto
        /// Valor: 1 (todos tienen ITBIS en sistemas de venta de celulares)
        /// </summary>
        public int TaxCategory { get; set; } = 1;

        /// <summary>
        /// Descuento por línea
        /// Origen: DetalleFactura.Descuento o null
        /// </summary>
        public decimal? Discount { get; set; }

        /// <summary>
        /// Sobrecargo por línea
        /// Origen: null por defecto
        /// </summary>
        public decimal? Surcharge { get; set; }

        /// <summary>
        /// Indicador de bien o servicio
        /// Valor: 1 = Bien (es sistema de venta de equipos/celulares)
        /// </summary>
        public int GoodOrServiceIndicator { get; set; } = 1;

        /// <summary>
        /// Unidad de medida
        /// Origen: null por defecto (unidades)
        /// </summary>
        public string UnitMeasure { get; set; }

        /// <summary>
        /// Monto retenido de ITBIS
        /// Origen: null por defecto
        /// </summary>
        public decimal? ItbisRetAmount { get; set; }

        /// <summary>
        /// Monto retenido de ISR
        /// Origen: null por defecto
        /// </summary>
        public decimal? IsrRetAmount { get; set; }

        /// <summary>
        /// Indicador de agente de retención
        /// Origen: null por defecto
        /// </summary>
        public int? RetentionAgentInd { get; set; }
    }
}
