using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MVSystemApi.Model
{
    public class Suplidor
    {
        public int ID_Suplidor { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public Int64 Numero_Telefono { get; set; }
        public int ID_Tipo_Telefono { get; set; }
        public int? Extencion { get; set; }
        public string Email_Detalle { get; set; }
        public string Direccion_Detalle { get; set; }
        public int ID_Tipo_Suplidor { get; set; }
        public int IDContacto { get; set; }
        public string Usuario { get; set; }
        public string Mensaje { get; set; }
    }
    public class Suplidor_Combo
    {
        public int ID_Suplidor { get; set; }
        public string Descripcion { get; set; }
    }
    public class Tipo_Suplidor_Combo
    {
        public int ID_Tipo_Suplidor { get; set; }
        public string Tipo_Suplidor_Descripcion { get; set; }
    }
}
