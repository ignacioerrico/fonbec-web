using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Fonbec.Web.Logic.Services;

public interface IRecipientMessageNotificationService
{
    /// <summary>
    /// Emails the student's current mediador about a message that is already stored.
    /// Failures are logged and swallowed so the sender's save still succeeds.
    /// </summary>
    Task NotifyFacilitatorOfRecipientMessageAsync(
        long recipientMessageId,
        CancellationToken cancellationToken = default);
}

public class RecipientMessageNotificationService(
    IRecipientMessageRepository repository,
    IEmailMessageSender emailMessageSender,
    IConfiguration configuration,
    TimeProvider timeProvider,
    ILogger<RecipientMessageNotificationService> logger) : IRecipientMessageNotificationService
{
    private const int MaxSendAttempts = 3;

    public async Task NotifyFacilitatorOfRecipientMessageAsync(
        long recipientMessageId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await NotifyCoreAsync(recipientMessageId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Facilitator notification for recipient message {RecipientMessageId} failed",
                recipientMessageId);
        }
    }

    private async Task NotifyCoreAsync(long recipientMessageId, CancellationToken cancellationToken)
    {
        var notice = await repository.GetFacilitatorNotificationAsync(recipientMessageId);
        if (notice is null)
        {
            logger.LogWarning(
                "Recipient message {RecipientMessageId} was not found for facilitator notification",
                recipientMessageId);
            return;
        }

        if (notice.FacilitatorNotifiedOn is not null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(notice.FacilitatorEmail))
        {
            logger.LogWarning(
                "Recipient message {RecipientMessageId} was saved but the facilitator has no email, so no notification was sent",
                recipientMessageId);
            return;
        }

        var baseUrl = configuration["App:BaseUrl"]?.Trim().TrimEnd('/');
        if (string.IsNullOrEmpty(baseUrl))
        {
            logger.LogError(
                "App:BaseUrl is not configured; facilitator notification for recipient message {RecipientMessageId} was not sent",
                recipientMessageId);
            return;
        }

        var messagesUrl = $"{baseUrl}/mensajes-para-becarios";
        var subject = RecipientMessageNotificationFormatter.BuildSubject(notice.StudentFullName);
        var html = RecipientMessageNotificationFormatter.BuildHtml(
            notice.StudentFullName,
            notice.StudentGender,
            notice.StudentFirstName,
            notice.StudentNickName,
            notice.IsCompany,
            notice.SenderGender,
            notice.SenderName,
            notice.Body,
            messagesUrl);
        var sent = false;

        for (var attempt = 1; attempt <= MaxSendAttempts; attempt++)
        {
            try
            {
                await emailMessageSender.SendEmailAsync(notice.FacilitatorEmail.Trim(), subject, html);
                sent = true;
                break;
            }
            catch (Exception ex) when (attempt < MaxSendAttempts && !cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(
                    ex,
                    "Facilitator notification for recipient message {RecipientMessageId} attempt {Attempt}/{MaxAttempts} failed",
                    recipientMessageId,
                    attempt,
                    MaxSendAttempts);

                await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt), cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Facilitator notification for recipient message {RecipientMessageId} failed after {MaxAttempts} attempts",
                    recipientMessageId,
                    MaxSendAttempts);
                return;
            }
        }

        if (!sent)
        {
            return;
        }

        try
        {
            await repository.MarkFacilitatorNotifiedAsync(
                recipientMessageId,
                timeProvider.GetUtcNow().UtcDateTime);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Email for recipient message {RecipientMessageId} was sent but FacilitatorNotifiedOn was not saved",
                recipientMessageId);
        }
    }
}