using Fonbec.Web.DataAccess.DataModels.Sponsors;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.ExtensionMethods;
using Mapster;

namespace Fonbec.Web.Logic.Models.Sponsors;

public class SponsorsListViewModel : AuditableViewModel, IDetectChanges<SponsorsListViewModel>
{
    public int SponsorId { get; set; }

    public string SponsorFirstName { get; set; } = string.Empty;

    public string SponsorLastName { get; set; } = string.Empty;

    public string SponsorNickName { get; set; } = string.Empty;

    public Gender SponsorGender { get; set; }

    public string SponsorPhoneNumber { get; set; } = string.Empty;

    public string SponsorEmail { get; set; } = string.Empty;

    public bool IsSponsorActive { get; set; }

    public bool IsIdenticalTo(SponsorsListViewModel other)
    {
        return SponsorFirstName == other.SponsorFirstName.NormalizeText()
               && SponsorLastName == other.SponsorLastName.NormalizeText()
               && SponsorNickName == other.SponsorNickName.NormalizeText()
               && SponsorGender == other.SponsorGender
               && SponsorEmail == other.SponsorEmail.Trim().ToLower()
               && SponsorPhoneNumber == other.SponsorPhoneNumber.NullOrTrimmed();
    }
}

public class SponsorsListViewModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<AllSponsorsDataModel, SponsorsListViewModel>()
            .Map(dest => dest.SponsorId, src => src.SponsorId)
            .Map(dest => dest.SponsorFirstName, src => src.SponsorFirstName)
            .Map(dest => dest.SponsorLastName, src => src.SponsorLastName)
            .Map(dest => dest.SponsorNickName, src => src.SponsorNickName ?? string.Empty)
            .Map(dest => dest.SponsorGender, src => src.SponsorGender)
            .Map(dest => dest.SponsorPhoneNumber, src => src.SponsorPhoneNumber ?? string.Empty)
            .Map(dest => dest.SponsorEmail, src => src.SponsorEmail)
            .Map(dest => dest.IsSponsorActive, src => src.IsSponsorActive);

        // Mapping required for the SponsorSelector component
        config.NewConfig<SponsorsListViewModel, SelectableModel<int>>()
            .Map(dest => dest.Key, src => src.SponsorId)
            .Map(dest => dest.DisplayName, src => $"{src.SponsorFirstName} {src.SponsorLastName}");
    }
}