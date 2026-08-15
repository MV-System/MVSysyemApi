namespace DTO
{
    /// <summary>
    /// DTO para consultar el historial de abonos/pagos de un cliente
    /// Basado en la tabla HISTORIO_PAGOS
    /// </summary>
    public class HistorialAbonoDTO
    {
        public int Id { get; set; }
        public int IdCliente { get; set; }
        public int NumeroFactura { get; set; }
        public decimal TotalFactura { get; set; }
        public decimal TotalAbono { get; set; }
        public decimal MontoPendiente { get; set; }
        public int IdTipoFactura { get; set; }
        public string TipoFactura { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public string Estado { get; set; }
        public string Usuario { get; set; }
        public DateTime FechaRegistro { get; set; }
        public int IdTipoPago { get; set; }
        public string TipoPago { get; set; }
        public string Nota { get; set; }
        public Guid? IdLoteAbono { get; set; }
    }
}
