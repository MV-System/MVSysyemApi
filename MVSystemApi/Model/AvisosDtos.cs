namespace MVSystemApi.Model
{
    /// <summary>Aviso o novedad tal como se lista o se muestra</summary>
    public class AvisoDto
    {
        public int IdAviso { get; set; }
        public string Tipo { get; set; }
        public string Prioridad { get; set; }
        public string Titulo { get; set; }
        public string Mensaje { get; set; }
        public string Donde { get; set; }
        public string Destino { get; set; }
        public string DestinoValor { get; set; }
        public string Permiso { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaVence { get; set; }
        public bool Activo { get; set; }
        public string CreadoPor { get; set; }
        public DateTime FechaCreacion { get; set; }
        /// <summary>Cuantos usuarios ya lo vieron (solo en el listado de administracion)</summary>
        public int Vistos { get; set; }
    }

    /// <summary>Lo que se envia para crear un aviso</summary>
    public class AvisoNuevoDto
    {
        public string Tipo { get; set; }
        public string Prioridad { get; set; }
        public string Titulo { get; set; }
        public string Mensaje { get; set; }
        public string Donde { get; set; }
        public string Destino { get; set; }
        public string DestinoValor { get; set; }
        public string Permiso { get; set; }
        /// <summary>Si no se indica, se muestra de inmediato</summary>
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaVence { get; set; }
    }

    public class AvisosVistosDto
    {
        public List<int> Ids { get; set; } = new List<int>();
    }

    public class AvisoVistoUsuarioDto
    {
        public string Usuario { get; set; }
        public string NombreUsuario { get; set; }
        public DateTime FechaVisto { get; set; }
    }
}
