using MVSystemApi.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTO
{
    public class AccesorioDTO: PagingDTO
    { 
        public Int64 ID { get; set; }
        public string Modelo { get; set; }
        public string Codigo { get; set; }
        public string DescripcionAccesorio { get; set; }
        public Int64? Cantidad { get; set; }
        public decimal PrecioPorMayor { get; set; }
        public decimal PrecioDetalle { get; set; }
        public decimal CostoEquipo { get; set; }
        public string Disponible { get; set; }
        public string DisponibleDetalle { get; set; }
        public Int64 CodBarra { get; set; }
        public string Almacen { get; set; }
        public decimal TotalInventario { get; set; }
        public string Estado { get; set; }
        public decimal TotalFacturado { get; set; }
        public decimal TotalGanancia { get; set; }
        public decimal TotalItbis { get; set; }
        public string Usuario { get; set; }

    }
    public class AccesorioVendidoDTO : PagingDTO
    {
        public Int64 ID { get; set; }
        public string Factura { get; set; }
        public string Descripcion { get; set; }
        public string Cliente { get; set; }
        public Int64? Cantidad { get; set; }
        public decimal Costo { get; set; } 
        public decimal Precio { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Descuento { get; set; }
        public decimal Itbis { get; set; }
        public decimal Total { get; set; }
        public string Usuario { get; set; }
        public DateTime FechaRegistro { get; set; }
        public string Vendedor { get; set; }
        public string Sucursal { get; set; }
        public decimal TotalInventario { get; set; }
        public decimal TotalFacturado { get; set; }
        public decimal TotalGanancia { get; set; }
        public decimal TotalItbis { get; set; }
    }
}
