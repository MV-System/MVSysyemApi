using System.ComponentModel.DataAnnotations;

namespace DTO
{
    public class DepartamentoResponseDTO
    {
        public int Codigo { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public int? Estado { get; set; }
        public DateTime? FechaRegistro { get; set; }
        public DateTime? FechaModificado { get; set; }
    }

    public class CrearDepartamentoDTO
    {
        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(50)]
        public string Nombre { get; set; }

        [StringLength(200)]
        public string Descripcion { get; set; }

        public int? Estado { get; set; } = 1;
    }

    public class ActualizarDepartamentoDTO
    {
        [Required(ErrorMessage = "El código es requerido")]
        public int Codigo { get; set; }

        [StringLength(50)]
        public string Nombre { get; set; }

        [StringLength(200)]
        public string Descripcion { get; set; }

        public int? Estado { get; set; }
    }
}
