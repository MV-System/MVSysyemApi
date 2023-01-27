using System;
using System.Collections.Generic;

namespace MVSystemApi.ModelsEF
{
    public partial class Usuario
    {
        public Usuario()
        {
            PermisoUsuarios = new HashSet<PermisoUsuario>();
            RolUsuarios = new HashSet<RolUsuario>();
        }

        public int Codigo { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public int Empresa { get; set; }
        public int Estado { get; set; }
        public DateTime FechaRegistro { get; set; }
        public DateTime? FechaModificado { get; set; }

        public virtual ICollection<PermisoUsuario> PermisoUsuarios { get; set; }
        public virtual ICollection<RolUsuario> RolUsuarios { get; set; }
    }
}
