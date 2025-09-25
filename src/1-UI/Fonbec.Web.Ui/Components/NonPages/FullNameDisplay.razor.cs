using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.NonPages;

public partial class FullNameDisplay
{
    [Parameter]
    public string FirstName { get; set; } = string.Empty;
    
    [Parameter]
    public string LastName { get; set; } = string.Empty;
    
    [Parameter]
    public bool IsLastNameFirst { get; set; }
}