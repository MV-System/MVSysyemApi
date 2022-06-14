using System;

namespace MVSystemApi.Model
{
    public class Clientes
    {
        public int IDContacto { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public int ID_Tipo_Cliente { get; set; }
        public string Tipo_Cliente_Descripcion { get; set; }
        public string Limite_Credito { get; set; }
        public Int64 Numero_Telefono { get; set; }
        public int ID_Tipo_Telefono { get; set; }
        public int? Extencion { get; set; }
        public string Email_Detalle { get; set; }
        public string Direccion_Detalle { get; set; }
        public string criterio { get; set; }
        public string Usuario { get; set; }
        public string Mensaje { get; internal set; }
    }
    public class Clientes_consulta
    {
        public int Id_Cliente { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public Int64 Numero_Telefono { get; set; }
        public string Mensaje { get; internal set; }
    }
}
