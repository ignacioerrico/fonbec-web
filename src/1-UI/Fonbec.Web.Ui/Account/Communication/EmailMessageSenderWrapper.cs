using Fonbec.Web.Logic.Util;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace Fonbec.Web.Ui.Account.Communication;

/// <summary>
/// Normally the class that implements ASP.NET Core Identity's IEmailSender would be defined in the Logic project,
/// but since the Interface comes from Microsoft.AspNetCore.Identity.UI, which is not referenced by Logic,
/// this wrapper class is defined here in the UI project and the actual logic is implemented in the Logic project.
/// </summary>
public class EmailMessageSenderWrapper(
    ILogger<EmailMessageSenderWrapper> logger,
    IEmailMessageSender emailMessageSender)
    : IEmailSender
{
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        logger.LogDebug("Sending email to {To} with subject '{Subject}' and body '{Body}'.",
            email,
            subject,
            htmlMessage);

        await emailMessageSender.SendEmailAsync(email, subject, htmlMessage);
    }
}