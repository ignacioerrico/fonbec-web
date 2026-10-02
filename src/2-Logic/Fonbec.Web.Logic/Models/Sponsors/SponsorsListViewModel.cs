using Fonbec.Web.DataAccess.DataModels.Sponsors;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models.Sponsorships;
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

    public int? SponsorCompanyId { get; set; }

    public string SponsorCompanyName { get; set; } = string.Empty;

    public string SponsorChapterName { get; set; } = string.Empty;

    public List<SponsoredStudentViewModel> SponsoredStudents { get; set; } = [];

    public List<SponsorListRecipientViewModel> SendAlsoTos { get; set; } = [];

    public bool IsIdenticalTo(SponsorsListViewModel other)
    {
        return SponsorFirstName == other.SponsorFirstName.NormalizeText()
               && SponsorLastName == other.SponsorLastName.NormalizeText()
               && SponsorNickName == other.SponsorNickName.NormalizeText()
               && SponsorGender == other.SponsorGender
               && SponsorEmail == other.SponsorEmail.Trim().ToLower()
               && SponsorPhoneNumber == other.SponsorPhoneNumber.Trim()
               && SponsorCompanyId == other.SponsorCompanyId;
    }
}

public class SponsoredStudentViewModel
{
    public string Name { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public SponsorshipTimelineStatus TimelineStatus =>
        SponsorshipTimeline.FromPeriod(StartDate, EndDate);

    public string PeriodTooltip =>
        EndDate is { } endDate
            ? $"{StartDate.ToSpanishMonthYear()} – {endDate.ToSpanishMonthYear()}"
            : $"Desde {StartDate.ToSpanishMonthYear()}";
}

public class SponsorListRecipientViewModel
{
    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool SendAsBcc { get; set; }

    public string DeliveryLabel => SendAsBcc ? "BCC" : "CC";
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
            .Map(dest => dest.SponsorCompanyId, src => src.SponsorCompany!.Id, srcCond => srcCond.SponsorCompany != null)
            .Map(dest => dest.SponsorCompanyName, src => src.SponsorCompany!.Name, srcCond => srcCond.SponsorCompany != null)
            .Map(dest => dest.SponsorCompanyName, src => string.Empty, srcCond => srcCond.SponsorCompany == null)
            .Map(dest => dest.SponsorEmail, src => src.SponsorEmail)
            .Map(dest => dest.IsSponsorActive, src => src.IsSponsorActive)
            .Map(dest => dest.SponsorChapterName, src => src.SponsorChapterName)
            .Map(dest => dest.SponsoredStudents, src => src.SponsoredStudents)
            .Map(dest => dest.SendAlsoTos, src => src.SendAlsoTos);

        config.NewConfig<SponsoredStudentDataModel, SponsoredStudentViewModel>()
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.StartDate, src => src.StartDate)
            .Map(dest => dest.EndDate, src => src.EndDate);

        config.NewConfig<SponsorListRecipientDataModel, SponsorListRecipientViewModel>()
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Email, src => src.Email)
            .Map(dest => dest.SendAsBcc, src => src.SendAsBcc);

        // Mapping required for the SponsorSelector component
        config.NewConfig<SponsorsListViewModel, SelectableModel<int>>()
            .Map(dest => dest.Key, src => src.SponsorId)
            .Map(dest => dest.DisplayName, src => $"{src.SponsorFirstName} {src.SponsorLastName}");
    }
}