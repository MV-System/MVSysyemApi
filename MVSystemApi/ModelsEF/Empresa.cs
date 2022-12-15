using System;
using System.Collections.Generic;

namespace MVSystemApi.ModelsEF
{
    public partial class Empresa
    {
        public int IdEmpresa { get; set; }
        public string NombreEmpresa { get; set; }
        public string StringCon { get; set; }
        public string Estado { get; set; }
        public string RegistroUsuario { get; set; }
        public DateTime Registrofecha { get; set; }
    }
}
