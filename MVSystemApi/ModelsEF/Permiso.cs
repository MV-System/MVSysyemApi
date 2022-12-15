using System;
using System.Collections.Generic;

namespace MVSystemApi.ModelsEF
{
    public partial class Permiso
    {
        public Permiso()
        {
            PermisoUsuarios = new HashSet<PermisoUsuario>();
            RolPermisos = new HashSet<RolPermiso>();
        }

        public int Id { get; set; }
        public string PermissionName { get; set; }

        public virtual ICollection<PermisoUsuario> PermisoUsuarios { get; set; }
        public virtual ICollection<RolPermiso> RolPermisos { get; set; }
    }
}
