using Fonbec.Web.Logic.Models.Sponsors;

namespace Fonbec.Web.Ui.Models.Sponsor;

public class SponsorRecipientBindModel
{
    public Guid TempId { get; set; } = Guid.NewGuid();

    public int Id { get; set; }

    public string RecipientName { get; set; } = string.Empty;

    public string RecipientEmail { get; set; } = string.Empty;

    public bool SendAsBcc { get; set; }

    public static bool CanAddRecipient(IReadOnlyList<SponsorRecipientBindModel> recipients, string? primaryEmail) =>
        recipients.All(recipient => IsCompleteAndValid(recipient, primaryEmail, recipients));

    public static bool IsCompleteAndValid(
        SponsorRecipientBindModel recipient,
        string? primaryEmail,
        IReadOnlyList<SponsorRecipientBindModel> recipients) =>
        !string.IsNullOrWhiteSpace(recipient.RecipientName)
        && !string.IsNullOrWhiteSpace(recipient.RecipientEmail)
        && SendAlsoToValidator.ValidateEmailField(
            recipient.RecipientEmail,
            primaryEmail,
            recipients.Where(other => other.TempId != recipient.TempId).Select(other => other.RecipientEmail)) is null;
}