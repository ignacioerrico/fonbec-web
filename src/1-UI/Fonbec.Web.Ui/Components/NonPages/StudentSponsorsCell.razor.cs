using Fonbec.Web.Logic.Models.Sponsorships;
using Fonbec.Web.Logic.Models.Students;
using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.NonPages;

public partial class StudentSponsorsCell
{
    [Parameter, EditorRequired]
    public int StudentId { get; set; }

    [Parameter, EditorRequired]
    public List<StudentActiveSponsorViewModel> Sponsors { get; set; } = [];

    /// <summary>
    /// Link to the student's sponsorships. When null the chips render as plain text,
    /// so callers can omit it for users who cannot open that page.
    /// </summary>
    [Parameter]
    public string? SponsorshipsHref { get; set; }

    private List<StudentActiveSponsorViewModel> ActiveSponsors =>
        Sponsors.Where(s => s.TimelineStatus == SponsorshipTimelineStatus.Active).ToList();

    private List<StudentActiveSponsorViewModel> UpcomingSponsors =>
        Sponsors
            .Where(s => s.TimelineStatus == SponsorshipTimelineStatus.NotStarted)
            .OrderBy(s => s.StartDate)
            .ToList();

    private List<StudentActiveSponsorViewModel> FinishedSponsors =>
        Sponsors
            .Where(s => s.TimelineStatus == SponsorshipTimelineStatus.Finished)
            .OrderBy(s => s.EndDate)
            .ToList();

    private bool ShowSinPadrino => ActiveSponsors.Count == 0 && UpcomingSponsors.Count == 0;
}
