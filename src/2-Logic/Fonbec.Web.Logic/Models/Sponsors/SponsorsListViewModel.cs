using Fonbec.Web.DataAccess.DataModels.Sponsors;
using Fonbec.Web.DataAccess.Entities.Enums;
using Mapster;

namespace Fonbec.Web.Logic.Models.Sponsors;

public class SponsorsListViewModel : AuditableViewModel
{
    public int SponsorId { get; set; }
    public string SponsorFirstName { get; set; } = string.Empty;
    public string SponsorLastName { get; set; } = string.Empty;
    public string SponsorNickName { get; set; } = string.Empty;
    public Gender SponsorGender { get; set; }
    public bool IsSponsorActive { get; set; }
    public string SponsorEmail { get; set; } = string.Empty;
    public string SponsorPhoneNumber { get; set; } = string.Empty;
}

public class SponsorsListViewModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<AllSponsorsDataModel, SponsorsListViewModel>()
            .Map(dest => dest.SponsorId, src => src.SponsorId)
            .Map(dest => dest.SponsorFirstName, src => src.SponsorFirstName)
            .Map(dest => dest.SponsorLastName, src => src.SponsorLastName)
            .Map(dest => dest.SponsorNickName, src => src.SponsorNickName)
            .Map(dest => dest.SponsorGender, src => src.SponsorGender)
            .Map(dest => dest.IsSponsorActive, src => src.IsSponsorActive)
            .Map(dest => dest.SponsorEmail, src => src.SponsorEmail)
            .Map(dest => dest.SponsorPhoneNumber, src => src.SponsorPhoneNumber)
            .IgnoreNonMapped(true);
    }
}