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
        public int? IdAlmacen{ get; set; }
        public int SubTotal { get; set; }
        public int Descuento { get; set; }
        public int Itbis { get; set; }
        public DateTime TiempoCredito { get; set; }
        public int Total { get; set; }
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
    }
    public class DetalleFactura
    {
        public int NumeroFactura { get; set; }
        //public int? IdAlmacen { get; set; }
        public int IdEquipo { get; set; }
        public int IdTipo { get; set; }
        public int IdVendedor { get; set; }
        public int IdTipoFactura { get; set; }
        public int IdGarantia { get; set; }
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
        public int SubTotal { get; set; }
        public int Descuento { get; set; }
        public int Itbis { get; set; }
        public int Total { get; set; }
        //public string Estado { get; set; }
        public string Usuario { get; set; }
        public DateTime FechaRegistro { get; set; }
        public string Descripcion { get; set; }
    }
}
