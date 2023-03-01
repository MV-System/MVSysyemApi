namespace MVSystemApi.Model
{
    public class CotizacionAmount
    {
        public int NumeroFactura { get; set; }
        public string Cliente { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Descuento { get; set; }
        public decimal Total { get; set; }
        public decimal Itbis { get; set; }
        public decimal Abono { get; set; }

    }
}
