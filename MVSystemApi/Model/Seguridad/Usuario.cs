namespace MVSystemApi.Model.Seguridad
{
    public class Usuario
    {
        public int Codigo { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public int Empresa { get; set; }
        public int Estado { get; set; }
        public DateTime FechaRegistro { get; set; }
        public DateTime FechaModificado { get; set; }
    }
}
