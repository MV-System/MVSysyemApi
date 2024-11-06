using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MVSystemApi.Model
{
    public class FacturaCliente
    {
        public Facturas Factura { get; set; }
        public List<FacturaReporte> FacturaReporte { get; set; }
        public Clientes_consulta Cliente { get; set; }
        public string VendedorNombre { get; set; }
        public DatosEmpresa DatosEmpresaFactura { get; internal set; }
    }

    public class FacturaReporte
    {
      
        public string PRODUCTO { get; set; }
        public int CANTIDAD_PROD { get; set; }
        public decimal PRECIO_PROD { get; set; }
        public decimal SUBTOTAL_PROD { get; set; }
        public decimal DESCUENTO_PROD { get; set; }
        public decimal TOTAL_PROD { get; set; }
    }
    public class FacturaFilter : Paginate
    {
        public int NumeroFactura { get; set; }
        public string Criterio { get; set; }
        public int Almacen { get; set; }
        public string TipoPago { get; set; }
        public string empresa { get; set; }
        public string IdEmpresa { get; set; }
        public string IdSucursal { get; set; }
        public int ComprobanteCheck { get; set; }

    }
    public class FacturaConsulta : PagingDTO
    {
        public string logo { get; set; }

        public int NumeroFactura { get; set; }
        public int ClienteId { get; set; }
        public long Telefono { get; set; }
        public string Ncf { get; set; }
        public string Rnc { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public string Vendedor { get; set; }
        public string TipoFactura { get; set; }
        public string Almacen { get; set; }
        public string Nota { get; set; }
        public string TipoPago { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Descuento { get; set; }
        public decimal Itbis { get; set; }
        public decimal Total { get; set; }
        public decimal TotalFacturado { get; set; }
        public DateTime FechaFacturacion { get; set; }
        public string empresa { get; set; }
    }
    public class Facturas
    {
        public string logo { get; set; }
        public int NumeroFactura { get; set; }
        public int IdCliente { get; set; }
        public int IdTipoFactura { get; set; }
        public string TipoFactura { get; set; }
        public int IdVendedor { get; set; }
        public int IdTipoPago { get; set; }
        public string TipoPago { get; set; }
        public int NcfTipoNumero { get; set; }
        public int Telefono { get; set; }
        public int? IdAlmacen { get; set; }
        public string Almacen { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Descuento { get; set; }
        public decimal Itbis { get; set; }
        public string TiempoCredito { get; set; }
        public decimal Total { get; set; }
        public int CantidadArticulos { get; set; }
        public string Nota { get; set; }
        public string Usuario { get; set; }
        public DateTime FechaRegistro { get; set; }
        public int Abono { get; set; }
        public string RNC { get; set; }
        public int NcfNumero { get; set; }
        public List<DetalleFactura> DetalleFacturaList { get; set; }

        public Clientes Cliente { get; set; }
        public FacturaConsulta Factura { get; internal set; }
        public string empresa { get; set; }
        public string IdEmpresa { get; set; }
        public string IdSucursal { get; set; }
        public DatosEmpresa DatosEmpresaFactura { get; internal set; }
    }

    public class DetalleFactura
    {
        public int NumeroFactura { get; set; }
        public string IdEquipo { get; set; }
        public int IdTipo { get; set; }
        public int IdVendedor { get; set; }
        public int IdTipoFactura { get; set; }
        public int IdGarantia { get; set; }
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Descuento { get; set; }
        public decimal Itbis { get; set; }
        public decimal Total { get; set; }
        public string Usuario { get; set; }
        public DateTime FechaRegistro { get; set; }
        public string Descripcion { get; set; }
    }
    public class FilterAnulaFactura
    {
        public int NumeroFactura { get; set; }
        public string Usuario { get; set; }
        public string IDsucursal { get; set; }
    }
}
