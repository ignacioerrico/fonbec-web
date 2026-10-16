using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.Email;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models;
using Fonbec.Web.Logic.Util;
using Microsoft.AspNetCore.Identity;

namespace Fonbec.Web.Ui.Account.Communication;

internal sealed class IdentityEmailSender(
    IEmailMessageSender emailMessageSender,
    IServiceScopeFactory scopeFactory) : IEmailSender<FonbecWebUser>
{
    public Task SendConfirmationLinkAsync(FonbecWebUser user, string email, string confirmationLink) =>
        SendAsync(user, email, EmailMessageKind.ConfirmEmail, "enlace-confirmación-correo", confirmationLink);

    public Task SendPasswordResetLinkAsync(FonbecWebUser user, string email, string resetLink) =>
        SendAsync(user, email, EmailMessageKind.ResetPassword, "enlace-restablecer-contraseña", resetLink);

    public Task SendPasswordResetCodeAsync(FonbecWebUser user, string email, string resetCode) =>
        SendAsync(user, email, EmailMessageKind.ResetPassword, "enlace-restablecer-contraseña", resetCode);

    private async Task SendAsync(
        FonbecWebUser user,
        string email,
        EmailMessageKind kind,
        string linkToken,
        string link)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var composer = scope.ServiceProvider.GetRequiredService<IEmailComposer>();
        var message = await composer.ComposeAsync(
            user.ChapterId,
            kind,
            new Dictionary<string, string?>
            {
                ["usuario"] = email,
                [linkToken] = link,
            });
        await emailMessageSender.SendEmailAsync(
            new Recipient(email, user.FullName()),
            message.Subject,
            message.HtmlBody);
    }
}
