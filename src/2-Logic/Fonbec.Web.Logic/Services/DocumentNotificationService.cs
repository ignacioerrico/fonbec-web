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

        foreach (var share in shares)
        {
            var historyUrl = $"{baseUrl}/padrinos/{share.PublicAccessToken}/{share.StudentId}";
            var subject = "Nuevo documento disponible";
            var html = DocumentNotificationMessageFormatter.BuildNotificationHtml(share, historyUrl);

            await emailMessageSender.SendEmailAsync(share.SponsorEmail, subject, html);
            await documentRepository.MarkShareNotifiedAsync(share.DocumentShareId, DateTime.UtcNow);
        }
    }
}
