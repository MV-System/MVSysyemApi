namespace MVSystemApi.Model
{
    public class Articulo
    {
        public int Cantidad { get; set; }
        public string Descripcion { get; set; }
        public decimal Descuento { get; set; }
        public string FechaRegistro { get; set; }
        // IMEI (equipos) o código del accesorio. Es texto porque un IMEI de 15 dígitos no cabe en un int.
        public string IdEquipo { get; set; }
        public int IdGarantia { get; set; }
        public int IdTipo { get; set; }
        public int NumeroFactura { get; set; }
        public decimal Precio { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Total { get; set; }
        public decimal Itbis { get; set; }
        public decimal PrecioDetalle { get; set; }
        public decimal PrecioMayor { get; set; }
    }

    public class CotizacionReporteModel
    {
        public CotizacionModel Cotizacion { get; set; }
        public DatosEmpresa DatosEmpresa { get; set; }
    }

    public class CotizacionModel
    {
        public string IdEmpresa { get; set; }
        public string IdSucursal { get; set; }
        public int NumeroFactura { get; set; }
        public int IdCliente { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public long Telefono { get; set; }
        public string Vendedor { get; set; }
        public int IdVendedor { get; set; }
        public int IdAlmacen { get; set; }
        public int IdTipoFactura { get; set; }
        public int IdTipoPago { get; set; }
        public string FechaFacturacion { get; set; }
        public string FechaRegistro { get; set; }
        public string FechaLimitePago { get; set; }
        public string NcfTipoNumero { get; set; }
        public string Rnc { get; set; }
        public int Garantia { get; set; }
        public string Nota { get; set; }
        public string Empresa { get; set; }
        public string Usuario { get; set; }
        public int CantidadArticulos { get; set; }
        public decimal Abono { get; set; }
        public object Codigo { get; set; }
        public object Cantidad { get; set; }
        public object Articulo { get; set; }
        public List<Articulo> Articulos { get; set; }
        public bool AplicarItbis { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Descuento { get; set; }
        public decimal Itbis { get; set; }
        public decimal Total { get; set; }
        public DatosEmpresa DatosEmpresaCotizacion { get; set; }
    }



}
