using System;
using System.Collections.Generic;

namespace MVSystemApi.ModelsEF
{
    public partial class RolUsuario
    {
        public int Id { get; set; }
        public int IdRol { get; set; }
        public int IdUser { get; set; }

        public virtual Role IdRolNavigation { get; set; }
        public virtual Usuario IdUserNavigation { get; set; }
    }
}
