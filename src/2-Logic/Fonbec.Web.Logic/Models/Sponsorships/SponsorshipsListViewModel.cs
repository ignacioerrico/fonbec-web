using Fonbec.Web.DataAccess.DataModels.Sponsorships;
using Fonbec.Web.Logic.ExtensionMethods;
using Mapster;

namespace Fonbec.Web.Logic.Models.Sponsorships;

public class SponsorshipsListViewModel
{
    public string StudentFullName { get; set; } = null!;

    public List<SponsorshipsSponsorshipsListViewModel> Sponsorships { get; set; } = [];
}

public enum SponsorshipTimelineStatus
{
    NotStarted,
    Active,
    Finished,
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
    public List<DateTime> LockedPlanStartsOn { get; set; } = [];

    public List<string> LockedPlanMonthLabels =>
        LockedPlanStartsOn.Select(d => d.ToSpanishMonthYear()).ToList();

    public bool IsCurrentlyActive => TimelineStatus == SponsorshipTimelineStatus.Active;

    public SponsorshipTimelineStatus TimelineStatus
    {
        get
        {
            var now = DateTime.UtcNow;
            if (SponsorshipStartDate > now)
            {
                return SponsorshipTimelineStatus.NotStarted;
            }

            if (SponsorshipEndDate is { } endDate && endDate < now)
            {
                return SponsorshipTimelineStatus.Finished;
            }

            return SponsorshipTimelineStatus.Active;
        }
    }

    public string SponsorshipStatusLabel => TimelineStatus switch
    {
        SponsorshipTimelineStatus.NotStarted => "No iniciado",
        SponsorshipTimelineStatus.Finished => "Finalizado",
        _ => "Activo",
    };
}

public class SponsorshipsListViewModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<AllSponsorshipsDataModel, SponsorshipsListViewModel>()
            .Map(dest => dest.StudentFullName, src => src.StudentFullName)
            .Map(dest => dest.Sponsorships,
                src => src.Sponsorships
                    .OrderBy(s => s.SponsorshipStartDate)
                    .ThenBy(s => s.SponsorshipEndDate));

        config.NewConfig<AllSponsorshipsSponsorshipsDataModel, SponsorshipsSponsorshipsListViewModel>()
            .Map(dest => dest.SponsorshipId, src => src.SponsorshipId)
            .Map(dest => dest.IsSponsoredByCompany, src => src.Sponsor == null && src.Company != null)
            .Map(dest => dest.SponsorshipFullName, src => src.Sponsor!.FullName(), srcCond => srcCond.Sponsor != null && srcCond.Company == null)
            .Map(dest => dest.SponsorshipFullName, src => src.Company!.Name, srcCond => srcCond.Sponsor == null && srcCond.Company != null)
            .Map(dest => dest.SponsorshipStartDate, src => src.SponsorshipStartDate)
            .Map(dest => dest.SponsorshipStartDateString, src => src.SponsorshipStartDate.ToSpanishMonthYear())
            .Map(dest => dest.SponsorshipEndDate, src => src.SponsorshipEndDate)
            .Map(dest => dest.SponsorshipEndDateString,
                src => src.SponsorshipEndDate.HasValue
                    ? src.SponsorshipEndDate.Value.ToSpanishMonthYear()
                    : "—")
            .Map(dest => dest.LockedPlanStartsOn, src => src.LockedPlanStartsOn);
    }
}