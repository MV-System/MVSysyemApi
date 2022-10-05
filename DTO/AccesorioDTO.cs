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

    }
}
