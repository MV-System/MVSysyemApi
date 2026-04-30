using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MVSystemApi.Model
{
    public class Tipos_Pagos
    {
        public int ID_Tipo_Pagos { get; set; }
        public string Descripcion_Tipo_Pagos { get; set; }
        public string Codigo_DGI { get; set; }  // Código del tipo de pago en catálogo DGI (ej: "01", "02", "03", etc.)

    }
}
