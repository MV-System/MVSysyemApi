using System.ComponentModel.DataAnnotations;

namespace DTO
{
    public class CrearPerfilDTO
    {
        [Required(ErrorMessage = "El nombre del perfil es requerido")]
        [StringLength(50)]
        public string Nombre { get; set; }

        [StringLength(200)]
        public string Descripcion { get; set; }

        [Required(ErrorMessage = "El sistema es requerido")]
        public int Sistema { get; set; }

        public int? Estado { get; set; } = 1;
    }

    public class ActualizarPerfilDTO
    {
        [Required(ErrorMessage = "El código del perfil es requerido")]
        public int Codigo { get; set; }

        [StringLength(50)]
        public string Nombre { get; set; }

        [StringLength(200)]
        public string Descripcion { get; set; }

        public int? Sistema { get; set; }

        public int? Estado { get; set; }
    }

    public class PerfilResponseDTO
    {
        public int Codigo { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public int? Sistema { get; set; }
        public string NombreSistema { get; set; }
        public int? Estado { get; set; }
        public DateTime? FechaRegistro { get; set; }
        public DateTime? FechaModificado { get; set; }
        public List<int> PermisosIds { get; set; } = new();
        public List<string> PermisosNombres { get; set; } = new();
    }
}
