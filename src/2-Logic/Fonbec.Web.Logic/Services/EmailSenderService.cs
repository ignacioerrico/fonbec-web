using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Logging;

namespace Fonbec.Web.Logic.Services;

public interface IEmailSenderService
{
    Task SendEmailAsync(EmailMessage emailMessage);
}

public class EmailSenderService(ILogger<EmailSenderService> logger, EmailClient emailClient) : IEmailSenderService
{
    public async Task SendEmailAsync(EmailMessage emailMessage)
    {
        if (emailMessage is null)
        {
            logger.LogError("EmailMessage parameter is null.");
            throw new ArgumentNullException(nameof(emailMessage));
        }

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