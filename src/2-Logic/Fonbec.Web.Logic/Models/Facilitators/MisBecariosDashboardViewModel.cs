using Fonbec.Web.DataAccess.DataModels.Facilitators;
using Fonbec.Web.DataAccess.Entities.Enums;
using Mapster;

namespace Fonbec.Web.Logic.Models.Facilitators;

public class MisBecariosDashboardViewModel
{
    public List<MisBecariosRowViewModel> Students { get; set; } = [];
}

public class MisBecariosRowViewModel
{
    public int StudentId { get; set; }

    public string StudentFirstName { get; set; } = null!;

    public string StudentLastName { get; set; } = null!;

    public string? StudentNickName { get; set; }

    public EducationLevel EducationLevel { get; set; }

    public List<DashboardSponsorViewModel> Sponsors { get; set; } = [];
}

public class DashboardSponsorViewModel
{
    public int SponsorshipId { get; set; }

    public int? SponsorId { get; set; }

    public int? CompanyId { get; set; }

    public string RecipientName { get; set; } = null!;

    public bool IsCompany { get; set; }
}

public class MisBecariosDashboardViewModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<MisBecariosDashboardDataModel, MisBecariosDashboardViewModel>()
            .Map(dest => dest.Students, src => src.Students);

        config.NewConfig<MisBecariosRowDataModel, MisBecariosRowViewModel>()
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
