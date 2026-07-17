using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Util;
using Microsoft.Extensions.Configuration;

namespace Fonbec.Web.Logic.Services;

public interface IDocumentNotificationService
{
    Task NotifySponsorsAsync(long documentId);
}

public class DocumentNotificationService(
    IDocumentRepository documentRepository,
    IEmailMessageSender emailMessageSender,
    IConfiguration configuration) : IDocumentNotificationService
{
    public async Task NotifySponsorsAsync(long documentId)
    {
        var shares = await documentRepository.GetUnnotifiedSharesAsync(documentId);
        var baseUrl = configuration["App:BaseUrl"]?.TrimEnd('/')
                      ?? throw new InvalidOperationException("App:BaseUrl is not configured.");

        const string subject = "Nuevo documento disponible";

        foreach (var share in shares)
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
        }
    }
}
