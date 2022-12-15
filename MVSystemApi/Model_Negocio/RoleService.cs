using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using MVSystemApi.Interfaz;
using MVSystemApi.ModelsEF;

namespace MVSystemApi.Model_Negocio
{
    public class RoleService : IRoleService
    {
        private readonly SEGURIDADContext _dbContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMemoryCache _memoryCache;

        public RoleService(SEGURIDADContext dbContext, IHttpContextAccessor httpContextAccessor, IMemoryCache memoryCache)
        {
            _dbContext = dbContext;
            _httpContextAccessor = httpContextAccessor;
            _memoryCache = memoryCache;
        }

        public async Task<List<string>> GetPermisos()
        {
            var key = $"Permisos:{_httpContextAccessor.HttpContext.User.Identity.Name}";
            var roles = _memoryCache.Get<List<string>>(key);

            if (roles == default)
            {
                roles = new List<string>();
                roles.AddRange(await _dbContext.Usuarios.Where(x => x.Login == _httpContextAccessor.HttpContext.User.Identity.Name).SelectMany(x => x.RolUsuarios.SelectMany(y => y.IdRolNavigation.RolPermisos.Select(x => x.IdPermisoNavigation.PermissionName))).ToListAsync());
                roles.AddRange(await _dbContext.Usuarios.Where(x => x.Login == _httpContextAccessor.HttpContext.User.Identity.Name).SelectMany(x => x.PermisoUsuarios.Select(x => x.IdPermisoNavigation.PermissionName)).ToListAsync());

                roles = roles.Distinct().ToList();
                _memoryCache.Set(key, roles, TimeSpan.FromMinutes(8));
            }

            return roles;
        }
    }
}
