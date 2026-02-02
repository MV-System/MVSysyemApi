using System.ComponentModel.DataAnnotations;

namespace DTO
{
    public class CrearPermisoDTO
    {
        [Required(ErrorMessage = "El nombre del permiso es requerido")]
        [StringLength(50)]
        public string Nombre { get; set; }

        [StringLength(200)]
        public string Descripcion { get; set; }

        [Required(ErrorMessage = "El módulo es requerido")]
        public int Modulo { get; set; }

        public int? Basico { get; set; } = 0;

        public int? Estado { get; set; } = 1;
    }

    public class ActualizarPermisoDTO
    {
        [Required(ErrorMessage = "El código del permiso es requerido")]
        public int Codigo { get; set; }

        [StringLength(50)]
        public string Nombre { get; set; }

        [StringLength(200)]
        public string Descripcion { get; set; }

        public int? Modulo { get; set; }

        public int? Basico { get; set; }

        public int? Estado { get; set; }
    }

    public class PermisoResponseDTO
    {
        public int Codigo { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public int? Modulo { get; set; }
        public string NombreModulo { get; set; }
        public int? Basico { get; set; }
        public int? Estado { get; set; }
        public DateTime? FechaRegistro { get; set; }
        public DateTime? FechaModificado { get; set; }
    }
}
