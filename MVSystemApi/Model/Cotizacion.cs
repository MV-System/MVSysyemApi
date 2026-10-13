namespace MVSystemApi.Model
{
    public class CotizacionConsultaFiltro : Paginate
    {
        public string Criterio { get; set; }
        public int Sucursal { get; set; }
        public int Vendedor { get; set; }
        public int IdTipoPago { get; set; }
        public string Estado { get; set; }
    }

    public class CotizacionConsulta : PagingDTO
    {
        public int Codigo { get; set; }
        public int NumeroFactura { get; set; }
        public int IdSucursal { get; set; }
        public string Literal { get; set; }
        public int ClienteId { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public string Telefono { get; set; }
        public string TipoFactura { get; set; }
        public string Vendedor { get; set; }
        public string TipoPago { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Descuento { get; set; }
        public decimal Itbis { get; set; }
        public decimal Abono { get; set; }
        public decimal Total { get; set; }
        public decimal TotalCotizado { get; set; }
        public string Nota { get; set; }
        public DateTime FechaFacturacion { get; set; }
        public string Estado { get; set; }
        public string Facturado { get; set; }
    }

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
