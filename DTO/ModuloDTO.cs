using System.ComponentModel.DataAnnotations;

namespace DTO
{
    public class ModuloResponseDTO
    {
        public int Codigo { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public int? Sistema { get; set; }
        public string NombreSistema { get; set; }
        public int? Estado { get; set; }
        public DateTime? FechaRegistro { get; set; }
        public DateTime? FechaModificado { get; set; }
    }

    public class CrearModuloDTO
    {
        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(50)]
        public string Nombre { get; set; }

        [StringLength(200)]
        public string Descripcion { get; set; }

        [Required(ErrorMessage = "El sistema es requerido")]
        public int Sistema { get; set; }

        public int? Estado { get; set; } = 1;
    }

    public class ActualizarModuloDTO
    {
        [Required(ErrorMessage = "El código es requerido")]
        public int Codigo { get; set; }

        [StringLength(50)]
        public string Nombre { get; set; }

        [StringLength(200)]
        public string Descripcion { get; set; }

        public int? Sistema { get; set; }

        public int? Estado { get; set; }
    }
}
