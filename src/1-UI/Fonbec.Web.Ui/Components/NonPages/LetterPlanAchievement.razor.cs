using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Fonbec.Web.Ui.Components.NonPages;

public partial class LetterPlanAchievement
{
    private ElementReference _originElement;
    private bool _confettiFired;

    [Inject]
    public IJSRuntime JsRuntime { get; set; } = null!;

    [Parameter, EditorRequired]
    public string PlanLabel { get; set; } = null!;

    [Parameter, EditorRequired]
    public int LettersDelivered { get; set; }

    [Parameter]
    public int ExemptStudents { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_confettiFired)
        {
            return;
        }

        try
        {
            // Retry across renders: firstRender can run before JS interop is available.
            var fired = await JsRuntime.InvokeAsync<bool>("fonbecBurstConfetti", _originElement);
            _confettiFired = fired;
        }
        catch (JSException)
        {
            // Script not loaded yet.
        }
        catch (InvalidOperationException)
        {
            // JS interop unavailable (e.g. prerender).
        }
    }
}