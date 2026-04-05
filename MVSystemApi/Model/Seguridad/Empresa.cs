namespace MVSystemApi.Model.Seguridad
{
    public class Empresa
    {
        public int ID_Empresa { get; set; }
        public string Nombre_Empresa { get; set; }
        public string StringCon { get; set; }
        public string Estado { get; set; }
        public string RegistroUsuario { get; set; }
        public DateTime RegistroFecha { get; set; }
    }
    public class EmpresaDTO
    {
        public int IdEmpresa { get; set; }
        public string NombreEmpresa { get; set; }
        public string NombreEmpresaMostrar { get; set; }
        public string Rnc { get; set; }
    }
}
