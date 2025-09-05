using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.NonPages;

public partial class LoadingIndicator
{
    [Parameter]
    public bool Loading { get; set; }

    [Parameter]
    public string? LoadingText { get; set; }

    [Parameter]
    public bool If { get; set; }

    [Parameter]
    public string ThenDisplay { get; set; } = string.Empty;

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        LoadingText ??= "Cargando datos";
    }
}