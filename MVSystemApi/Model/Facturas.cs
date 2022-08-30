using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MVSystemApi.Model
{
    public class Facturas
    {
        //public string Cliente { get; set; }
        public int NumeroFactura { get; set; }
        public int IdCliente { get; set; }
        public int IdTipoFactura { get; set; }
        public int IdVendedor { get; set; }
        public int IdTipoPago { get; set; }
        public int NcfTipoNumero { get; set; }
        public int? IdAlmacen { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Descuento { get; set; }
        public decimal Itbis { get; set; }
        public string TiempoCredito { get; set; }
        public decimal Total { get; set; }
        public int CantidadArticulos { get; set; }
        public string Nota { get; set; }
        //public string Estado { get; set; }
        public string Usuario { get; set; }
        public string FechaRegistro { get; set; }
        public int Abono { get; set; }
        public string RNC { get; set; }
        //public string NcfFiscal { get; set; }
        public int NcfNumero { get; set; }
        public List<DetalleFactura> DetalleFacturaList { get; set; }
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
        public string FechaRegistro { get; set; }
        public string Descripcion { get; set; }
    }
}
