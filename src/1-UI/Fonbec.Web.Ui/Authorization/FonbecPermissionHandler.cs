using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Authorization;

namespace Fonbec.Web.Ui.Authorization;

internal class FonbecPermissionHandler(IUserService userService) : AuthorizationHandler<FonbecPermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, FonbecPermissionRequirement requirement)
    {
        var fonbecAuthValue = userService.GetFonbecAuthClaim(context.User);

        if (fonbecAuthValue is null)
        {
            return Task.CompletedTask;
        }

        var hasPermission = userService.HasPermission(fonbecAuthValue, requirement.PageName);

        if (hasPermission)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}