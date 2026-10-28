using System;

namespace MVSystemApi.Model
{
    public class EquipoConfiguracion
    {
        public int? ID_Configuracion { get; set; }
        public string Imei { get; set; }
        public int? Ram_GB { get; set; }
        public string Ram_Tipo { get; set; }
        public int? Almacenamiento_GB { get; set; }
        public string Almacenamiento_Tipo { get; set; }
        public string Notas { get; set; }
        public string Usuario { get; set; }
        public string Configuracion { get; set; }
        public string Mensaje { get; set; }
    }
}
