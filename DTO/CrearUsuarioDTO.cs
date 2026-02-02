using System.ComponentModel.DataAnnotations;

namespace DTO
{
    public class CrearUsuarioDTO
    {
        [Required(ErrorMessage = "El nombre es requerido")]
        [StringLength(50)]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "El login es requerido")]
        [StringLength(30)]
        public string Login { get; set; }

        [Required(ErrorMessage = "La contraseña es requerida")]
        [StringLength(200, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        public string Password { get; set; }

        [Required(ErrorMessage = "El departamento es requerido")]
        public int Departamento { get; set; }

        [StringLength(200)]
        public string Descripcion { get; set; }

        public int? Perfil { get; set; }

        public int? Id_Sucursal { get; set; }

        public int? Estado { get; set; } = 1; // Activo por defecto
    }

    public class ActualizarUsuarioDTO
    {
        [Required(ErrorMessage = "El código de usuario es requerido")]
        public int Codigo { get; set; }

        [StringLength(50)]
        public string Nombre { get; set; }

        [StringLength(200)]
        public string Descripcion { get; set; }

        public int? Perfil { get; set; }

        public int? Departamento { get; set; }

        public int? Id_Sucursal { get; set; }

        public int? Estado { get; set; }
    }

    public class CambioContrasenaDTO
    {
        [Required(ErrorMessage = "El código de usuario es requerido")]
        public int CodigoUsuario { get; set; }

        [Required(ErrorMessage = "La contraseña actual es requerida")]
        public string ContraseñaActual { get; set; }

        [Required(ErrorMessage = "La contraseña nueva es requerida")]
        [StringLength(200, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        public string ContraseñaNueva { get; set; }

        [Required(ErrorMessage = "Debe confirmar la contraseña")]
        public string ConfirmarContraseña { get; set; }
    }

    public class UsuarioResponseDTO
    {
        public int Codigo { get; set; }
        public string Nombre { get; set; }
        public string Login { get; set; }
        public string Descripcion { get; set; }
        public int? Perfil { get; set; }
        public string NombrePerfil { get; set; }
        public int? Departamento { get; set; }
        public string NombreDepartamento { get; set; }
        public int? Estado { get; set; }
        public int? Id_Sucursal { get; set; }
        public DateTime? FechaRegistro { get; set; }
        public DateTime? FechaModificado { get; set; }
    }
}
