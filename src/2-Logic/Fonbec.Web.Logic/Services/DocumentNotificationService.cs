using Fonbec.Web.DataAccess.DataModels.Documents;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Fonbec.Web.Logic.Services;

public interface IDocumentNotificationService
{
    Task NotifySponsorsAsync(long documentId);

    Task NotifyChapterManagersPlanReadyAsync(
        int chapterId,
        int planId,
        DateTime planStartsOn);
}

public class DocumentNotificationService(
    IDocumentRepository documentRepository,
    IUserRepository userRepository,
    IEmailMessageSender emailMessageSender,
    IConfiguration configuration,
    ILogger<DocumentNotificationService> logger) : IDocumentNotificationService
{
    private const int MaxSendAttempts = 3;

    public async Task NotifySponsorsAsync(long documentId)
    {
        var shares = await documentRepository.GetUnnotifiedSharesAsync(documentId);
        var baseUrl = configuration["App:BaseUrl"]?.TrimEnd('/')
                      ?? throw new InvalidOperationException("App:BaseUrl is not configured.");

        const string subject = "Nuevo documento disponible";

        foreach (var share in shares)
        {
            await NotifyShareAsync(documentId, share, baseUrl, subject);
        }
    }

    public async Task NotifyChapterManagersPlanReadyAsync(
        int chapterId,
        int planId,
        DateTime planStartsOn)
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

        foreach (var manager in managers)
        {
            await SendWithRetryAsync(manager.Email, subject, html, planId);
        }
    }

    private async Task NotifyShareAsync(
        long documentId,
        DocumentShareNotificationDataModel share,
        string baseUrl,
        string subject)
    {
        for (var attempt = 1; attempt <= MaxSendAttempts; attempt++)
        {
            try
            {
                // Companies and person-sponsors are notified identically, each linking to its own
                // public history page. A company with no email address is simply skipped (still marked
                // notified so it isn't reprocessed); the document remains available on its history page.
                if (!string.IsNullOrWhiteSpace(share.RecipientEmail))
                {
                    var segment = share.IsCompany ? "empresas" : "padrinos";
                    var historyUrl = $"{baseUrl}/{segment}/{share.PublicAccessToken}/{share.StudentId}";
                    var html = DocumentNotificationMessageFormatter.BuildNotificationHtml(share, historyUrl);

                    await emailMessageSender.SendEmailAsync(share.RecipientEmail, subject, html);
                }

                await documentRepository.MarkShareNotifiedAsync(share.DocumentShareId, DateTime.UtcNow);
                return;
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

                await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt));
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

    private async Task SendWithRetryAsync(string email, string subject, string html, int planId)
    {
        for (var attempt = 1; attempt <= MaxSendAttempts; attempt++)
        {
            try
            {
                await emailMessageSender.SendEmailAsync(email, subject, html);
                return;
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

                await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt));
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
}
