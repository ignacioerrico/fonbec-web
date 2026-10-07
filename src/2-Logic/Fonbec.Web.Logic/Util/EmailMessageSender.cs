using Azure;
using Azure.Communication.Email;
using Fonbec.Web.Logic.Builders;
using Fonbec.Web.Logic.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Fonbec.Web.Logic.Util;

public interface IEmailMessageSender
{
    Task SendEmailAsync(string email, string subject, string htmlMessage);

    Task SendEmailAsync(
        IReadOnlyList<Recipient> to,
        IReadOnlyList<Recipient> cc,
        IReadOnlyList<Recipient> bcc,
        string subject,
        string htmlMessage);
}

public class EmailMessageSender(
    IConfiguration configuration,
    ILogger<EmailMessageSender> logger,
    EmailClient emailClient)
    : IEmailMessageSender
{
    public Task SendEmailAsync(string email, string subject, string htmlMessage) =>
        SendEmailAsync([new Recipient(email)], [], [], subject, htmlMessage);

    public async Task SendEmailAsync(
        IReadOnlyList<Recipient> to,
        IReadOnlyList<Recipient> cc,
        IReadOnlyList<Recipient> bcc,
        string subject,
        string htmlMessage)
    {
        var emailMessageBuilder = new EmailMessageBuilder(configuration, to.ToList(), subject, htmlMessage);
        emailMessageBuilder.Cc.AddRange(cc);
        emailMessageBuilder.Bcc.AddRange(bcc);

        var emailMessage = emailMessageBuilder.Build();

        try
        {
            // Started returns once Azure has accepted the message. Waiting until Completed
            // polls until delivery processing finishes and blocks the caller for seconds.
            var emailSendOperation = await emailClient.SendAsync(
                WaitUntil.Started,
                emailMessage);

            logger.LogDebug("Email accepted for delivery. OperationId = {OperationId}", emailSendOperation?.Id);
        }
        catch (RequestFailedException ex)
        {
            // OperationID is contained in the exception message and can be used for troubleshooting purposes
            logger.LogError("Email send operation failed with error code: {ErrorCode}, message: {Message}", ex.ErrorCode, ex.Message);
            throw;
        }
    }
}