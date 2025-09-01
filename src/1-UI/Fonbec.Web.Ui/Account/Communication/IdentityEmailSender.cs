using Fonbec.Web.DataAccess.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace Fonbec.Web.Ui.Account.Communication;

internal sealed class IdentityEmailSender(IEmailSender emailSender) : IEmailSender<FonbecWebUser>
{
    public async Task SendConfirmationLinkAsync(FonbecWebUser user, string email, string confirmationLink)
    {
        await emailSender.SendEmailAsync(email, "Confirmá tu correo electrónico", $"Confirmá tu correo electrónico <a href='{confirmationLink}'>haciendo clic acá</a>.");
    }

    public async Task SendPasswordResetLinkAsync(FonbecWebUser user, string email, string resetLink)
    {
        await emailSender.SendEmailAsync(email, "Reseteá tu contraseña", $"Reseteá tu contraseña <a href='{resetLink}'>haciendo clic acá</a>.");
    }

    public async Task SendPasswordResetCodeAsync(FonbecWebUser user, string email, string resetCode)
    {
        await emailSender.SendEmailAsync(email, "Reseteá tu contraseña", $"Reseteá tu contraseña usando este código: {resetCode}");
    }
}