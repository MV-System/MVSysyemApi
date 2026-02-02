using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace DTO
{
    public class AsignacionPermisoDTO
    {
        [Required(ErrorMessage = "El código del usuario es requerido")]
        [JsonPropertyName("codigoUsuario")]
        public int CodigoUsuario { get; set; }

        [Required(ErrorMessage = "Debe seleccionar al menos un permiso")]
        [JsonPropertyName("permisosIds")]
        public List<int> PermisosIds { get; set; } = new();
    }

    public class AsignacionPerfilDTO
    {
        [Required(ErrorMessage = "El código del usuario es requerido")]
        [JsonPropertyName("codigoUsuario")]
        public int CodigoUsuario { get; set; }

        [Required(ErrorMessage = "El código del perfil es requerido")]
        [JsonPropertyName("codigoPerfil")]
        public int CodigoPerfil { get; set; }
    }

    public class DesasignacionPermisoDTO
    {
        [Required(ErrorMessage = "El código del usuario es requerido")]
        [JsonPropertyName("codigoUsuario")]
        public int CodigoUsuario { get; set; }

        [Required(ErrorMessage = "El código del permiso es requerido")]
        [JsonPropertyName("codigoPermiso")]
        public int CodigoPermiso { get; set; }
    }

    public class DesasignacionPerfilDTO
    {
        [Required(ErrorMessage = "El código del usuario es requerido")]
        [JsonPropertyName("codigoUsuario")]
        public int CodigoUsuario { get; set; }
    }

    public class UsuarioConPermisosDTO
    {
        public int Codigo { get; set; }
        public string Nombre { get; set; }
        public string Login { get; set; }
        public int? Estado { get; set; }
        public int? Perfil { get; set; }
        public string NombrePerfil { get; set; }
        public List<int> PermisosDirectosIds { get; set; } = new();
        public List<string> PermisosDirectosNombres { get; set; } = new();
        public List<int> PermisosHereadosIds { get; set; } = new();
        public List<string> PermisosHereadosNombres { get; set; } = new();
    }
}
