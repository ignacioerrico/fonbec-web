using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.Pages.Sponsors;

public partial class SponsorDocumentHistory
{
    [Parameter]
    public Guid Token { get; set; }

    [Parameter]
    public int StudentId { get; set; }
}