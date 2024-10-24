namespace MVSystemApi.Model
{
    public class DatosEmpresa
    {
        public int IdEmpresa { get;  set; }
        public string Empresa { get; internal set; }
        public string Direccion { get; internal set; }
        public string Telefonos { get; internal set; }
        public string Encabezado { get; internal set; }
        public string CondicionesGarantia { get; internal set; }
        public string RNC { get; internal set; }
        public string StringCon { get; internal set; }
    }
}
