using Fonbec.Web.DataAccess.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Fonbec.Web.Ui.Account;

public sealed class FonbecSignInManager(
    UserManager<FonbecWebUser> userManager,
    IHttpContextAccessor contextAccessor,
    IUserClaimsPrincipalFactory<FonbecWebUser> claimsFactory,
    IOptions<IdentityOptions> optionsAccessor,
    ILogger<SignInManager<FonbecWebUser>> logger,
    IAuthenticationSchemeProvider schemes,
    IUserConfirmation<FonbecWebUser> confirmation)
    : SignInManager<FonbecWebUser>(
        userManager,
        contextAccessor,
        claimsFactory,
        optionsAccessor,
        logger,
        schemes,
        confirmation)
{
    public override async Task SignInAsync(
        FonbecWebUser user,
        AuthenticationProperties properties,
        string? authenticationMethod = null)
    {
        await base.SignInAsync(user, properties, authenticationMethod);

        if (Context.Response.HasStarted)
        {
            return;
        }

        // Account preference wins over a stale cookie on this browser.
        ThemeCookie.Set(Context.Response, user.IsDarkMode);
    }
}