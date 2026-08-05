using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.NonPages;

public partial class LetterPlanAchievement
{
    private const int ConfettiCount = 24;

    private bool _showConfetti;

    [Parameter, EditorRequired]
    public string PlanLabel { get; set; } = null!;

    [Parameter, EditorRequired]
    public int LettersDelivered { get; set; }

    [Parameter]
    public int ExemptStudents { get; set; }

    protected override void OnAfterRender(bool firstRender)
    {
        // Prerender would play (and finish) the animation before the UI is interactive.
        if (firstRender)
        {
            _showConfetti = true;
            StateHasChanged();
        }
    }
}