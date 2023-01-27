using System;
using System.Collections.Generic;

namespace MVSystemApi.ModelsEF
{
    public partial class PermisoUsuario
    {
        public int Id { get; set; }
        public int IdPermiso { get; set; }
        public int IdUser { get; set; }

        public virtual Permiso IdPermisoNavigation { get; set; }
        public virtual Usuario IdUserNavigation { get; set; }
    }
}
