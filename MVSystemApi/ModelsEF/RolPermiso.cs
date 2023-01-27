using System;
using System.Collections.Generic;

namespace MVSystemApi.ModelsEF
{
    public partial class RolPermiso
    {
        public int Id { get; set; }
        public int IdPermiso { get; set; }
        public int IdRole { get; set; }

        public virtual Permiso IdPermisoNavigation { get; set; }
        public virtual Role IdRoleNavigation { get; set; }
    }
}
