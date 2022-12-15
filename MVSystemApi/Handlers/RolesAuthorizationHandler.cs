using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using MVSystemApi.ModelsEF;
using Microsoft.EntityFrameworkCore;

namespace MVSystemApi.Handlers
{
    public class RolesAuthorizationHandler : AuthorizationHandler<RolesAuthorizationRequirement>, IAuthorizationHandler
    {
        private readonly SEGURIDADContext _dbContext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RolesAuthorizationHandler(SEGURIDADContext dbContext, IHttpContextAccessor httpContextAccessor)
        {
            _dbContext = dbContext;
            _httpContextAccessor = httpContextAccessor;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, RolesAuthorizationRequirement requirement)
        {
            var valid = false;

            if (requirement?.AllowedRoles?.Any() == true)
            {
                var roles = new List<string>();
                roles.AddRange(await _dbContext.Usuarios.Where(x => x.Login == _httpContextAccessor.HttpContext.User.Identity.Name).SelectMany(x => x.RolUsuarios.SelectMany(y => y.IdRolNavigation.RolPermisos.Select(x => x.IdPermisoNavigation.PermissionName))).ToListAsync());
                //roles.AddRange(await _dbContext.Usuarios.Where(x => x.Login == _httpContextAccessor.HttpContext.User.Identity.Name).SelectMany(x => x.PermisoUsuarios.Select(x => x.IdPermisoNavigation.PermissionName)).ToListAsync());

                roles = roles.Distinct().ToList();

                valid = requirement.AllowedRoles.Any(x => roles.Contains(x));
            }

            if (valid)
                context.Succeed(requirement);
            else
                context.Fail();
        }
    }
}
