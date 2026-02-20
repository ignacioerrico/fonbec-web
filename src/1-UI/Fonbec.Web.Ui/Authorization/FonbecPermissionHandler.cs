using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Authorization;

namespace Fonbec.Web.Ui.Authorization;

/// <summary>
/// Authorization handler
/// 
/// To understand this code, read:
/// https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies?view=aspnetcore-10.0#authorization-handlers
/// </summary>
/// <param name="userService"></param>
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