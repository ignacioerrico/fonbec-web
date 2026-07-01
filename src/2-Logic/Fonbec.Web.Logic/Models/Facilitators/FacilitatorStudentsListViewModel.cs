using Fonbec.Web.DataAccess.DataModels.Facilitators;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.ExtensionMethods;
using Mapster;

namespace Fonbec.Web.Logic.Models.Facilitators;

public class FacilitatorStudentsListViewModel : AuditableViewModel, IDetectChanges<FacilitatorStudentsListViewModel>
{
    public int StudentId { get; set; }

    public string StudentFirstName { get; set; } = null!;

    public string StudentLastName { get; set; } = null!;

    public string StudentNickName { get; set; } = string.Empty;

    public EducationLevel EducationLevel { get; set; }

    public List<DashboardSponsorViewModel> Sponsors { get; set; } = [];

    public bool IsIdenticalTo(FacilitatorStudentsListViewModel other) =>
        StudentFirstName == other.StudentFirstName.NormalizeText()
        && StudentLastName == other.StudentLastName.NormalizeText()
        && StudentNickName == other.StudentNickName.NormalizeText()
        && EducationLevel == other.EducationLevel
        && Sponsors.Count == other.Sponsors.Count
        && Sponsors.All(s => other.Sponsors.Any(os =>
            os.SponsorshipId == s.SponsorshipId
            && os.SponsorId == s.SponsorId
            && os.CompanyId == s.CompanyId));
}

public class DashboardSponsorViewModel
{
    public int SponsorshipId { get; set; }

    public int? SponsorId { get; set; }

    public int? CompanyId { get; set; }

    public string RecipientName { get; set; } = null!;

    public bool IsCompany { get; set; }
}

public class FacilitatorStudentsListViewModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<FacilitatorStudentsDataModel, FacilitatorStudentsListViewModel>()
            .Map(dest => dest.StudentId, src => src.StudentId)
            .Map(dest => dest.StudentFirstName, src => src.StudentFirstName)
            .Map(dest => dest.StudentLastName, src => src.StudentLastName)
            .Map(dest => dest.StudentNickName, src => src.StudentNickName)
            .Map(dest => dest.EducationLevel, src => src.EducationLevel)
            .Map(dest => dest.Sponsors, src => src.Sponsors);

        config.NewConfig<DashboardSponsorDataModel, DashboardSponsorViewModel>()
            .Map(dest => dest.SponsorshipId, src => src.SponsorshipId)
            .Map(dest => dest.SponsorId, src => src.SponsorId)
            .Map(dest => dest.CompanyId, src => src.CompanyId)
            .Map(dest => dest.RecipientName, src => src.RecipientName)
            .Map(dest => dest.IsCompany, src => src.IsCompany);
    }
}
