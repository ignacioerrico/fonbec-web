using Fonbec.Web.DataAccess.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace Fonbec.Web.Ui.Account.Communication;

// Remove the "else if (EmailSender is IdentityNoOpEmailSender)" block from RegisterConfirmation.razor after updating with a real implementation.
internal sealed class IdentityNoOpEmailSender : IEmailSender<FonbecWebUser>
{
    private readonly IEmailSender _emailSender = new NoOpEmailSender();

    public Task SendConfirmationLinkAsync(FonbecWebUser user, string email, string confirmationLink) =>
        _emailSender.SendEmailAsync(email, "Confirmá tu correo electrónico", $"Confirmá tu cuenta <a href='{confirmationLink}'>haciendo clic acá</a>.");

    public Task SendPasswordResetLinkAsync(FonbecWebUser user, string email, string resetLink) =>
        _emailSender.SendEmailAsync(email, "Restablecé tu contraseña", $"Restablecé tu contraseña <a href='{resetLink}'>haciendo clic acá</a>.");

    public Task SendPasswordResetCodeAsync(FonbecWebUser user, string email, string resetCode) =>
        _emailSender.SendEmailAsync(email, "Restablecé tu contraseña", $"Restablecé tu contraseña usando este código: {resetCode}");
}
