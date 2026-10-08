namespace Fonbec.Web.DataAccess.Entities;

/// <summary>
/// A plain-text message a person-sponsor or company wrote to a student from the anonymous history page.
/// The app does not deliver it to the student. A mediador marks <see cref="SharedOn"/> when they have passed it on.
/// </summary>
public class RecipientMessage
{
    public long RecipientMessageId { get; set; }

    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    /// <summary>Person-sponsor. Mutually exclusive with <see cref="CompanyId"/>.</summary>
    public int? SponsorId { get; set; }
    public Sponsor? Sponsor { get; set; }

    /// <summary>Company. Mutually exclusive with <see cref="SponsorId"/>.</summary>
    public int? CompanyId { get; set; }
    public Company? Company { get; set; }

    public string Body { get; set; } = "";

    public DateTime SentOn { get; set; }

    /// <summary>Null while Pendiente. Set by US 134. Cleared on undo.</summary>
    public DateTime? SharedOn { get; set; }

    public int? SharedById { get; set; }
    public FonbecWebUser? SharedBy { get; set; }
}