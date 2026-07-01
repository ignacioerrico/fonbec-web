using Fonbec.Web.DataAccess.Entities.Abstract;
using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.DataAccess.DataModels.Facilitators;

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
