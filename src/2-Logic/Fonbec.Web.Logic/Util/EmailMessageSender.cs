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
}

public class EmailMessageSender(
    IConfiguration configuration,
    ILogger<EmailMessageSender> logger,
    EmailClient emailClient)
    : IEmailMessageSender
{
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        List<Recipient> recipients = [new(email)];

        var emailMessageBuilder = new EmailMessageBuilder(configuration, recipients, subject, htmlMessage);

        var emailMessage = emailMessageBuilder.Build();

        try
        {
            var emailSendOperation = await emailClient.SendAsync(
                WaitUntil.Completed,
                emailMessage);

            if (emailSendOperation.HasValue)
            {
                logger.LogDebug("Email queued for delivery. Status = {Status}", emailSendOperation.Value.Status);
            }
            else
            {
                logger.LogWarning("Email send operation completed but no value was returned. OperationId = {OperationId}", emailSendOperation.Id);
            }

            // The OperationId can be used for tracking the message for troubleshooting
            logger.LogDebug("Email operation id = {OperationId}", emailSendOperation.Id);
        }
        catch (RequestFailedException ex)
        {
            // OperationID is contained in the exception message and can be used for troubleshooting purposes
            logger.LogError("Email send operation failed with error code: {ErrorCode}, message: {Message}", ex.ErrorCode, ex.Message);
            throw;
        }
    }
}