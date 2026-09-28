using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.Pages.Companies;

public partial class CompanyDocumentHistory
{
    [Parameter]
    public Guid Token { get; set; }

    [Parameter]
    public int StudentId { get; set; }
}