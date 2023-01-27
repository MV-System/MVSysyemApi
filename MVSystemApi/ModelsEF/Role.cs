using System;
using System.Collections.Generic;

namespace MVSystemApi.ModelsEF
{
    public partial class Role
    {
        public Role()
        {
            RolPermisos = new HashSet<RolPermiso>();
            RolUsuarios = new HashSet<RolUsuario>();
        }

        public int Id { get; set; }
        public string RoleName { get; set; }

        public virtual ICollection<RolPermiso> RolPermisos { get; set; }
        public virtual ICollection<RolUsuario> RolUsuarios { get; set; }
    }
}
