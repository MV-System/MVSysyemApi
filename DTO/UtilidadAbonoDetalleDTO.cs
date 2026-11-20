namespace DTO
{
    public class UtilidadAbonoDetalleDTO
    {
        public int Id { get; set; }
        public int IdCliente { get; set; }
        public string Cliente { get; set; }
        public int NumeroFactura { get; set; }
        public DateTime FechaAbono { get; set; }
        public decimal MontoAbono { get; set; }
        public decimal RetencionAbono { get; set; }
        // En el abono que salda la factura: retencion de todos sus abonos con tarjeta
        public decimal RetencionFactura { get; set; }
        public bool FacturaSaldada { get; set; }
        public bool EsAbonoQueSaldaFactura { get; set; }
        public decimal CostoFactura { get; set; }
        public decimal TotalFactura { get; set; }
        public decimal GananciaAbono { get; set; }
        public string Usuario { get; set; }
        public string Nota { get; set; }
    }
}
