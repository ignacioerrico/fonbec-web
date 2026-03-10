using Fonbec.Web.DataAccess.DataModels.Sponsorships;
using Mapster;

namespace Fonbec.Web.Logic.Models.Sponsorships;

public class SponsorshipsListViewModel : AuditableViewModel
{
    public int StudentId { get; set; }
    public int? SponsorId { get; set; }
    public int? CompanyId { get; set; }
    public DateTime SponsorshipStartDate { get; set; }
    public DateTime? SponsorshipEndDate { get; set; }
    public string SponsorshipNotes { get; set; } = string.Empty;
    public string SponsorFullName { get; set; } = string.Empty;
    public string StudentFullName { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string SponsorOrCompanyName => 
                  SponsorId.HasValue? SponsorFullName : CompanyName;
    public bool IsCompany => CompanyId.HasValue;
}

public class SponsorshipsListViewModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<AllSponsorshipsDataModel, SponsorshipsListViewModel>()
            .Map(dest => dest.SponsorId, src => src.SponsorId)
            .Map(dest => dest.StudentId, src => src.StudentId)
            .Map(dest => dest.CompanyId, src => src.CompanyId)
            .Map(dest => dest.SponsorshipStartDate, src => src.SponsorshipStartDate)
            .Map(dest => dest.SponsorshipEndDate, src => src.SponsorshipEndDate)
            .Map(dest => dest.SponsorshipNotes, src => src.SponsorshipNotes)
            .Map(dest => dest.CreatedBy, src => src.CreatedBy)
            .Map(dest => dest.SponsorFullName, src => src.SponsorFullName)
            .Map(dest => dest.StudentFullName, src => src.StudentFullName)
            .Map(dest => dest.CompanyName, src => src.CompanyName);
    }
}