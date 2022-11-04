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
    }

    public class FacturaReporte
    {
        //public string FACTURA_NUMERO { get; set; }
        //public string NCF { get; set; }
        //public string RNC { get; set; }
        //public string FECHA { get; set; }
        //public string FECHA_VENCE { get; set; }
        //public string ID_VEND { get; set; }
        //public string VENDEDOR { get; set; }
        //public string TIPO_FACTURA { get; set; }
        //public string TIPO_PAGO { get; set; }
        //public string ID_CLI { get; set; }
        //public string CLIENTE { get; set; }
        //public string DIRECCION { get; set; }
        //public string TELEFONO { get; set; }
        //public string SUBTOTAL_FACT { get; set; }
        //public string DESCUENTO_FACT { get; set; }
        //public string ITBIS_FACT { get; set; }
        //public string TOTAL_FACT { get; set; }
        //public string ABONADO { get; set; }
        //public string NOTA { get; set; }
        //public string ID_PROD { get; set; }
        public string PRODUCTO { get; set; }
        //public string MARCA { get; set; }
        //public string MODELO { get; set; }
        public int CANTIDAD_PROD { get; set; }
        public decimal PRECIO_PROD { get; set; }
        public decimal SUBTOTAL_PROD { get; set; }
        public decimal DESCUENTO_PROD { get; set; }
        public decimal TOTAL_PROD { get; set; }
        //public decimal ITBIS_PROD { get; set; }
        //public string NFCTipoDescripcion { get; set; }
    }
    public class FacturaFilter : Paginate
    {
        public int NumeroFactura { get; set; }
        public string Criterio { get; set; }
        public int Almacen { get; set; }
        public string TipoPago { get; set; }
    }
    public class FacturaConsulta : PagingDTO
    {
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
    }
    public class Facturas
    {
        //public string Cliente { get; set; }
        public int NumeroFactura { get; set; }
        public int IdCliente { get; set; }
        public int IdTipoFactura { get; set; }
        public int IdVendedor { get; set; }
        public int IdTipoPago { get; set; }
        public int NcfTipoNumero { get; set; }
        public int Telefono { get; set; }
        public int? IdAlmacen { get; set; }
        public string? Almacen { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Descuento { get; set; }
        public decimal Itbis { get; set; }
        public string TiempoCredito { get; set; }
        public decimal Total { get; set; }
        public int CantidadArticulos { get; set; }
        public string Nota { get; set; }
        //public string Estado { get; set; }
        public string Usuario { get; set; }
        public DateTime FechaRegistro { get; set; }
        public int Abono { get; set; }
        public string RNC { get; set; }
        //public string NcfFiscal { get; set; }
        public int NcfNumero { get; set; }
        public List<DetalleFactura> DetalleFacturaList { get; set; }

        public Clientes Cliente { get; set; }
        public FacturaConsulta Factura { get; internal set; }
    }

    public class DetalleFactura
    {
        public int NumeroFactura { get; set; }
        //public int? IdAlmacen { get; set; }
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
        //public string Estado { get; set; }
        public string Usuario { get; set; }
        public DateTime FechaRegistro { get; set; }
        public string Descripcion { get; set; }
    }
}
