using Fonbec.Web.Logic.Models.Sponsors;
using Fonbec.Web.Logic.Models.Sponsorships;
using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.NonPages;

public partial class SponsorStudentsCell
{
    [Parameter, EditorRequired]
    public List<SponsoredStudentViewModel> Students { get; set; } = [];

    private List<SponsoredStudentViewModel> ActiveStudents =>
        Students
            .Where(s => s.TimelineStatus == SponsorshipTimelineStatus.Active)
            .ToList();

    private List<SponsoredStudentViewModel> UpcomingStudents =>
        Students
            .Where(s => s.TimelineStatus == SponsorshipTimelineStatus.NotStarted)
            .OrderBy(s => s.StartDate)
            .ToList();

    private List<SponsoredStudentViewModel> FinishedStudents =>
        Students
            .Where(s => s.TimelineStatus == SponsorshipTimelineStatus.Finished)
            .OrderBy(s => s.EndDate)
            .ToList();
}