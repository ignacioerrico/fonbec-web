using Azure.Communication.Email;
using Fonbec.Web.Logic.Models;
using Microsoft.Extensions.Configuration;
using System.Net.Mime;

namespace Fonbec.Web.Logic.Builders;

public class EmailMessageBuilder
{
    private readonly string _from;

    public EmailMessageBuilder(IConfiguration configuration,
        List<Recipient> to,
        string subject,
        string bodyHtml)
    {
        _from = configuration.GetSection("Email:From").Value
                ?? throw new NullReferenceException("Email:From not set.");

        var replyToEmail = configuration.GetSection("Email:ReplyToEmail").Value;
        if (replyToEmail is not null && !string.IsNullOrWhiteSpace(replyToEmail))
        {
            var replyToDisplayName = configuration.GetSection("Email:ReplyToDisplayName").Value;
            var displayName = replyToDisplayName is null || string.IsNullOrWhiteSpace(replyToDisplayName)
                ? replyToEmail
                : replyToDisplayName;
            ReplyTo = new Recipient(replyToEmail, displayName);
        }

        To = to;
        Subject = subject;
        BodyHtml = bodyHtml;
    }

    public List<Recipient> To { get; }

    public List<Recipient> Cc { get; } = [];

    public List<Recipient> Bcc { get; } = [];

    public Recipient? ReplyTo { get; }

    public string Subject { get; }

    public string BodyHtml { get; }

    public string? BodyPlainText { get; set; }

    public List<string> FilePaths { get; } = [];

    public EmailMessage Build()
    {
        var emailContent = new EmailContent(Subject)
        {
            Html = BodyHtml,
            PlainText = BodyPlainText
        };

        var to = To.Select(r => new EmailAddress(r.EmailAddress, r.DisplayName));
        var cc = Cc.Any()
            ? Cc.Select(r => new EmailAddress(r.EmailAddress, r.DisplayName))
            : null;
        var bcc = Bcc.Any()
            ? Bcc.Select(r => new EmailAddress(r.EmailAddress, r.DisplayName))
            : null;
        var emailRecipients = new EmailRecipients(to, cc, bcc);

        EmailMessage emailMessage;
        if (ReplyTo is null)
        {
            emailMessage = new EmailMessage(_from, emailRecipients, emailContent);
        }
        else
        {
            emailMessage = new EmailMessage(_from, emailRecipients, emailContent)
            {
                ReplyTo = { new EmailAddress(ReplyTo.EmailAddress, ReplyTo.DisplayName) }
            };
        }

        foreach (var filePath in FilePaths)
        {
            var fileInfo = new FileInfo(filePath);
            var fileName = fileInfo.Name;
            var contentType = fileInfo.Extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase)
                ? MediaTypeNames.Application.Pdf
                : MediaTypeNames.Application.Octet;

            var bytes = File.ReadAllBytes(filePath);
            var binaryData = new BinaryData(bytes);
            var emailAttachment = new EmailAttachment(fileName, contentType, binaryData);

            emailMessage.Attachments.Add(emailAttachment);
        }

        return emailMessage;
    }
}