namespace Fonbec.Web.DataAccess.DataModels.Facilitators;

public class MisBecariosDashboardDataModel
{
    public List<MisBecariosRowDataModel> Students { get; set; } = [];
}

public class MisBecariosRowDataModel
{
    public int StudentId { get; set; }

    public string StudentFirstName { get; set; } = null!;

    public string StudentLastName { get; set; } = null!;

    public string? StudentNickName { get; set; }

    public Entities.Enums.EducationLevel EducationLevel { get; set; }

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
