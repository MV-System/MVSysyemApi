namespace DTO
{
    // Alias de Perfiles para RolesController
    public class CrearRolDTO : CrearPerfilDTO { }
    public class ActualizarRolDTO : ActualizarPerfilDTO { }
    public class RolResponseDTO : PerfilResponseDTO { }
    
    // Alias para Asignaciones
    public class AsignacionRolDTO : AsignacionPerfilDTO { }
    public class DesasignacionRolDTO : DesasignacionPerfilDTO { }
}
