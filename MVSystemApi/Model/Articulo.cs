namespace MVSystemApi.Model
{
    public class Articulo
    {
        public int Cantidad { get; set; }
        public string Descripcion { get; set; }
        public int Descuento { get; set; }
        public string FechaRegistro { get; set; }
        public int IdEquipo { get; set; }
        public int IdGarantia { get; set; }
        public int IdTipo { get; set; }
        public int NumeroFactura { get; set; }
        public int Precio { get; set; }
        public int SubTotal { get; set; }
        public int Total { get; set; }
        public int Itbis { get; set; }
        public int PrecioDetalle { get; set; }
        public int PrecioMayor { get; set; }
    }


    public class CotizacionModel
    {
        public int IdCliente { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public long Telefono { get; set; }
        public string Vendedor { get; set; }
        public object Almacen { get; set; }
        public int TipoFactura { get; set; }
        public int TipoPago { get; set; }
        public string FechaFacturacion { get; set; }
        public string Comprobante { get; set; }
        public int Garantia { get; set; }
        public string Nota { get; set; }
        public string Empresa { get; set; }
        public object Codigo { get; set; }
        public string Rnc { get; set; }
        public object Cantidad { get; set; }
        public object Articulo { get; set; }
        public List<Articulo> Articulos { get; set; }
        public bool AplicarItbis { get; set; }
    }



}
