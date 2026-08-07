using Fonbec.Web.DataAccess.Entities.Abstract;
using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.DataAccess.DataModels.Facilitators;

/// <summary>
/// The current (latest, non-superseded) letter for one slot (student + sponsor/company) in a
/// given plan. Absence of a row for a slot means no letter has been uploaded yet (us111).
/// </summary>
public class SponsorLetterStatusDataModel
{
    public int StudentId { get; set; }

    public int? SponsorId { get; set; }

    public int? CompanyId { get; set; }

    public DocumentStatus Status { get; set; }

    public string? RejectionReason { get; set; }
}

public class FacilitatorStudentsDataModel(Auditable auditable) : AuditableDataModel(auditable)
{
    public int StudentId { get; set; }

    public string StudentFirstName { get; set; } = null!;

    public string StudentLastName { get; set; } = null!;

    public string? StudentNickName { get; set; }

    public EducationLevel EducationLevel { get; set; }

    public List<DashboardSponsorDataModel> Sponsors { get; set; } = [];
}

public class DashboardSponsorDataModel
{
    public int SponsorshipId { get; set; }

    public int? SponsorId { get; set; }

    public int? CompanyId { get; set; }

    public string RecipientName { get; set; } = null!;

    public bool IsCompany { get; set; }
}