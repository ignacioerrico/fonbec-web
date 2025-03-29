using Fonbec.Web.DataAccess.Entities;
using Microsoft.AspNetCore.Identity;

namespace Fonbec.Web.Ui.Account;

internal sealed class IdentityUserAccessor(UserManager<FonbecWebUser> userManager, IdentityRedirectManager redirectManager)
{
    public async Task<FonbecWebUser> GetRequiredUserAsync(HttpContext context)
    {
        var user = await userManager.GetUserAsync(context.User);

        if (user is null)
        {
            redirectManager.RedirectToWithStatus("Account/InvalidUser", $"Error: Unable to load user with ID '{userManager.GetUserId(context.User)}'.", context);
        }

        return user;
    }
}
