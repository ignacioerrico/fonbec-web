using Fonbec.Web.DataAccess.DataModels.Sponsorships;
using Mapster;

namespace Fonbec.Web.Logic.Models.Sponsorships;

public class SponsorshipsListViewModel
{
    public string StudentFullName { get; set; } = null!;

    public List<SponsorshipsSponsorshipsListViewModel> Sponsorships { get; set; } = [];
}

public class SponsorshipsSponsorshipsListViewModel : AuditableViewModel
{
    public int SponsorshipId { get; set; }
    public bool IsSponsoredByCompany { get; set; }
    public string SponsorshipFullName { get; set; } = null!;
    public DateTime SponsorshipStartDate { get; set; }
    public string SponsorshipStartDateString { get; set; } = null!;
    public DateTime? SponsorshipEndDate { get; set; }
    public string SponsorshipEndDateString { get; set; } = null!;
}

public class SponsorshipsListViewModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<AllSponsorshipsDataModel, SponsorshipsListViewModel>()
            .Map(dest => dest.StudentFullName, src => src.StudentFullName)
            .Map(dest => dest.Sponsorships, src => src.Sponsorships);

        config.NewConfig<AllSponsorshipsSponsorshipsDataModel, SponsorshipsSponsorshipsListViewModel>()
            .Map(dest => dest.SponsorshipId, src => src.SponsorshipId)
            .Map(dest => dest.IsSponsoredByCompany, src => src.Sponsor == null && src.Company != null)
            .Map(dest => dest.SponsorshipFullName, src => src.Sponsor!.FullName(), srcCond => srcCond.Sponsor != null && srcCond.Company == null)
            .Map(dest => dest.SponsorshipFullName, src => src.Company!.Name, srcCond => srcCond.Sponsor == null && srcCond.Company != null)
            .Map(dest => dest.SponsorshipStartDate, src => src.SponsorshipStartDate)
            .Map(dest => dest.SponsorshipStartDateString, src => src.SponsorshipStartDate.ToString("MM/yyyy"))
            .Map(dest => dest.SponsorshipEndDate, src => src.SponsorshipEndDate)
            .Map(dest => dest.SponsorshipEndDateString, src => src.SponsorshipEndDate.HasValue ? src.SponsorshipEndDate.Value.ToString("MM/yyyy") : "—");
    }
}