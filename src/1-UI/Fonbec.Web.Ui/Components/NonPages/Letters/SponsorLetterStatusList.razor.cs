using Fonbec.Web.Logic.Models.Students;
using Fonbec.Web.Ui.Constants;
using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.NonPages.Letters;

public partial class SponsorLetterStatusList
{
    [Parameter]
    [EditorRequired]
    public IReadOnlyList<SponsorLetterStatusViewModel> Sponsors { get; set; } = [];

    private static string LetterStatusText(SponsorLetterStatusViewModel sponsor) =>
        sponsor.Status == LetterSlotStatus.Rejected && !string.IsNullOrWhiteSpace(sponsor.RejectionReason)
            ? $"Rechazada: {sponsor.RejectionReason}"
            : sponsor.Status.Label();
}