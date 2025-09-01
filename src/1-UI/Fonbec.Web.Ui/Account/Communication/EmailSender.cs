using Fonbec.Web.Logic.Builders;
using Fonbec.Web.Logic.Models;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace Fonbec.Web.Ui.Account.Communication;

public class EmailSender(
    ILogger<EmailSender> logger,
    IConfiguration configuration,
    IEmailSenderService emailSenderService)
    : IEmailSender
{
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        logger.LogDebug("Sending email to {To} with subject '{Subject}' and body '{Body}'.",
            email,
            subject,
            htmlMessage);

        List<Recipient> recipients = [new(email)];

        var emailMessageBuilder = new EmailMessageBuilder(configuration, recipients, subject, htmlMessage);

        var emailMessage = emailMessageBuilder.Build();

        await emailSenderService.SendEmailAsync(emailMessage);
    }
}