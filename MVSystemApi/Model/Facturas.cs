using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MVSystemApi.Model
{
    public class Facturas
    {
        public string Cliente { get; set; }
        public int Numero_Factura { get; set; }
        public int ID_Cliente { get; set; }
        public int ID_Tipo_Factura { get; set; }
        public int ID_Vendedor { get; set; }
        public int ID_Tipo_Pagos { get; set; }
        public int NFCTipoNumero { get; set; }
        public int? ID_Sucursal { get; set; }
        public string SubTotal { get; set; }
        public string Descuento { get; set; }
        public string Itbis { get; set; }
        public DateTime Tiempo_Credito { get; set; }
        public string Total { get; set; }
        public int Cantidad_Articulos { get; set; }
        public string Nota { get; set; }
        public string Estado { get; set; }
        public string Usuario { get; set; }
        public DateTime Fecha_registro { get; set; }
        public string abono { get; set; }
        public string RNC { get; set; }
        public string NFCFISCAL { get; set; }
        public string NCF_NUMERO { get; set; }
        public List<Detalle_Factura> DetalleFacturaList { get; set; }
    }
    public class Detalle_Factura
    {
        public int Numero_Factura { get; set; }
        public int? ID_Sucursal { get; set; }
        public string ID_Equipo { get; set; }
        public int ID_Tipo { get; set; }
        public int ID_Vendedor { get; set; }
        public int ID_Tipo_Factura { get; set; }
        public int ID_Garantia { get; set; }
        public int cantidad { get; set; }
        public decimal precio { get; set; }
        public string SubTotal { get; set; }
        public string Descuento { get; set; }
        public string Itbis { get; set; }
        public string Total { get; set; }
        public string Estado { get; set; }
        public string Usuario { get; set; }
        public DateTime Fecha_registro { get; set; }
        public string Descripcion { get; set; }
    }
}
