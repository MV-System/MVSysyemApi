using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using MVSystemApi.Interfaz;

namespace MVSystemApi.Handlers
{
    public class RolesAuthorizationHandler : AuthorizationHandler<RolesAuthorizationRequirement>, IAuthorizationHandler
    {
        private readonly IRoleService _roleService;

        public RolesAuthorizationHandler(IRoleService roleService)
        {
            _roleService = roleService;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, RolesAuthorizationRequirement requirement)
        {
            var valid = false;

            if (requirement?.AllowedRoles?.Any() == true)
            {
                var roles = await _roleService.GetPermisos();
                valid = requirement.AllowedRoles.Any(x => roles.Contains(x));
            }

            if (valid)
                context.Succeed(requirement);
            else
                context.Fail();
        }
    }
}
