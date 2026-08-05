using Fonbec.Web.Logic.Models.Students;
using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.NonPages.Letters;

public partial class SponsorLetterStatusList
{
    [Parameter]
    [EditorRequired]
    public IReadOnlyList<SponsorLetterStatusViewModel> Sponsors { get; set; } = [];
}
