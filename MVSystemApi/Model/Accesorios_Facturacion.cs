using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MVSystemApi.Model
{
    public class Accesorios_Facturacion
    {
        public string codigo { get; set; }
        public int Id { get; set; }
        public string Accesorio_Descripcion { get; set; }
        public string Modelo { get; set; }
        public Int64 Codigo_Barra { get; set; }
        public decimal Precio_Por_Mayor { get; set; }
        public decimal Precio_Detalle { get; set; }
    }
}
