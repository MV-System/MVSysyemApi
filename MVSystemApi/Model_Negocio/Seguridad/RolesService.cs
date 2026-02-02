using DTO;

namespace MVSystemApi.Model_Negocio.Seguridad
{
    /// <summary>
    /// RolesService es un alias de PerfilesService para mantener compatibilidad
    /// El nombre "Rol" se mapea a "Perfil" en la base de datos
    /// </summary>
    public class RolesService : PerfilesService
    {
        public RolesService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor) 
            : base(configuration, httpContextAccessor) 
        {
        }
    }
}
