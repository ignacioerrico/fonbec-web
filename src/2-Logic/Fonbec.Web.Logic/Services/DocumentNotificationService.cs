using Fonbec.Web.DataAccess.DataModels.Documents;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models;
using Fonbec.Web.Logic.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Fonbec.Web.Logic.Services;

public interface IDocumentNotificationService
{
    Task NotifySponsorsAsync(long documentId, CancellationToken cancellationToken = default);

    Task NotifyChapterManagersPlanReadyAsync(
        int chapterId,
        int planId,
        DateTime planStartsOn,
        CancellationToken cancellationToken = default);
}

public class DocumentNotificationService(
    IDocumentRepository documentRepository,
    IUserRepository userRepository,
    IEmailMessageSender emailMessageSender,
    IConfiguration configuration,
    ILogger<DocumentNotificationService> logger) : IDocumentNotificationService
{
    private const int MaxSendAttempts = 3;
    private const int MaxConcurrentSends = 4;

    public async Task NotifySponsorsAsync(long documentId, CancellationToken cancellationToken = default)
    {
        var shares = await documentRepository.GetUnnotifiedSharesAsync(documentId);
        var baseUrl = configuration["App:BaseUrl"]?.TrimEnd('/')
                      ?? throw new InvalidOperationException("App:BaseUrl is not configured.");

        const string subject = "Nuevo documento disponible";

        await ForEachWithConcurrencyAsync(
            shares,
            share => NotifyShareAsync(documentId, share, baseUrl, subject, cancellationToken),
            cancellationToken);
    }

    public async Task NotifyChapterManagersPlanReadyAsync(
        int chapterId,
        int planId,
        DateTime planStartsOn,
        CancellationToken cancellationToken = default)
    {
        var managers = await userRepository.GetChapterManagerContactsAsync(chapterId);
        if (managers.Count == 0)
        {
            return;
        }

        var baseUrl = configuration["App:BaseUrl"]?.TrimEnd('/')
                      ?? throw new InvalidOperationException("App:BaseUrl is not configured.");

        var planLabel = planStartsOn.ToSpanishMonthYear();
        var progressUrl = $"{baseUrl}/planificaciones/{planId}/cartas";
        var subject = "Campaña lista para completar";
        var html = DocumentNotificationMessageFormatter.BuildPlanReadyHtml(planLabel, progressUrl);

        await ForEachWithConcurrencyAsync(
            managers,
            manager => SendWithRetryAsync(manager.Email, subject, html, planId, cancellationToken),
            cancellationToken);
    }

    private static async Task ForEachWithConcurrencyAsync<T>(
        IEnumerable<T> items,
        Func<T, Task> action,
        CancellationToken cancellationToken)
    {
        using var gate = new SemaphoreSlim(MaxConcurrentSends);
        var tasks = items.Select(async item =>
        {
            await gate.WaitAsync(cancellationToken);
            try
            {
                await action(item);
            }
            finally
            {
                gate.Release();
            }
        });

        await Task.WhenAll(tasks);
    }

    private async Task NotifyShareAsync(
        long documentId,
        DocumentShareNotificationDataModel share,
        string baseUrl,
        string subject,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxSendAttempts; attempt++)
        {
            try
            {
                // A share with no primary address is not emailed. CC/BCC are copies of that mail,
                // not a substitute To, and the share is still marked notified so it is not retried.
                // The document remains available on the recipient's history page.
                if (!string.IsNullOrWhiteSpace(share.RecipientEmail))
                {
                    var segment = share.IsCompany ? "empresas" : "padrinos";
                    var historyUrl = $"{baseUrl}/{segment}/{share.PublicAccessToken}/{share.StudentId}";
                    var html = DocumentNotificationMessageFormatter.BuildNotificationHtml(share, historyUrl);
                    var (to, cc, bcc) = BuildRecipients(share);

                    await emailMessageSender.SendEmailAsync(to, cc, bcc, subject, html);
                }

                await documentRepository.MarkShareNotifiedAsync(share.DocumentShareId, DateTime.UtcNow);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (attempt < MaxSendAttempts)
            {
                logger.LogWarning(
                    ex,
                    "Document {DocumentId} share {DocumentShareId} notification attempt {Attempt}/{MaxAttempts} failed",
                    documentId,
                    share.DocumentShareId,
                    attempt,
                    MaxSendAttempts);

                await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt), cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Document {DocumentId} share {DocumentShareId} notification failed after {MaxAttempts} attempts; leaving unmarked for retry",
                    documentId,
                    share.DocumentShareId,
                    MaxSendAttempts);
            }
        }
    }

    private async Task SendWithRetryAsync(
        string email,
        string subject,
        string html,
        int planId,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxSendAttempts; attempt++)
        {
            try
            {
                await emailMessageSender.SendEmailAsync(email, subject, html);
                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (attempt < MaxSendAttempts)
            {
                logger.LogWarning(
                    ex,
                    "Plan-ready notification to {Email} for plan {PlanId} attempt {Attempt}/{MaxAttempts} failed",
                    email,
                    planId,
                    attempt,
                    MaxSendAttempts);

                await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt), cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Plan-ready notification to {Email} for plan {PlanId} failed after {MaxAttempts} attempts",
                    email,
                    planId,
                    MaxSendAttempts);
            }
        }
    }

    private (IReadOnlyList<Recipient> To, IReadOnlyList<Recipient> Cc, IReadOnlyList<Recipient> Bcc) BuildRecipients(
        DocumentShareNotificationDataModel share)
    {
        IReadOnlyList<Recipient> to = [new Recipient(share.RecipientEmail)];
        if (share.IsCompany || share.AdditionalRecipients.Count == 0)
        {
            return (to, [], []);
        }

        var primaryEmail = NormalizeEmail(share.RecipientEmail);
        var chosen = new Dictionary<string, AdditionalRecipientChoice>(StringComparer.Ordinal);

        foreach (var extra in share.AdditionalRecipients)
        {
            var email = extra.RecipientEmail?.Trim() ?? string.Empty;
            if (email.Length == 0 || !ContactFieldValidator.IsValidEmail(email))
            {
                LogSkippedAdditionalRecipient(share.DocumentShareId, extra.RecipientEmail, extra.SendAsBcc);
                continue;
            }

            var normalized = NormalizeEmail(email);
            if (normalized.Length == 0 || normalized == primaryEmail)
            {
                continue;
            }

            if (chosen.TryGetValue(normalized, out var existing))
            {
                if (extra.SendAsBcc && !existing.SendAsBcc)
                {
                    chosen[normalized] = existing with { SendAsBcc = true };
                }

                continue;
            }

            var displayName = string.IsNullOrWhiteSpace(extra.RecipientName) ? null : extra.RecipientName.Trim();
            chosen[normalized] = new AdditionalRecipientChoice(email, displayName, extra.SendAsBcc);
        }

        var cc = new List<Recipient>();
        var bcc = new List<Recipient>();
        foreach (var extra in chosen.Values)
        {
            var recipient = extra.DisplayName is null
                ? new Recipient(extra.Email)
                : new Recipient(extra.Email, extra.DisplayName);

            if (extra.SendAsBcc)
            {
                bcc.Add(recipient);
            }
            else
            {
                cc.Add(recipient);
            }
        }

        return (to, cc, bcc);
    }

    private void LogSkippedAdditionalRecipient(long documentShareId, string? email, bool sendAsBcc)
    {
        // BCC addresses stay off Information and above. CC skips are operational and include the address.
        if (sendAsBcc)
        {
            logger.LogWarning(
                "Skipping invalid BCC recipient for document share {DocumentShareId}",
                documentShareId);
            logger.LogDebug(
                "Skipped BCC address {Email} for document share {DocumentShareId}",
                email,
                documentShareId);
            return;
        }

        logger.LogWarning(
            "Skipping invalid CC recipient {Email} for document share {DocumentShareId}",
            email,
            documentShareId);
    }

    private static string NormalizeEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? string.Empty : email.Trim().ToLower();

    private readonly record struct AdditionalRecipientChoice(string Email, string? DisplayName, bool SendAsBcc);
}