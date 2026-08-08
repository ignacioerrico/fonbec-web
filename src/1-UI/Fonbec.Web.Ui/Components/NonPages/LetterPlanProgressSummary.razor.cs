using Fonbec.Web.Logic.Models.LetterPlanProgress;
using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.NonPages;

public partial class LetterPlanProgressSummary
{
    [Parameter, EditorRequired]
    public LetterPlanProgressSummaryViewModel Summary { get; set; } = null!;

    [Parameter, EditorRequired]
    public string Title { get; set; } = null!;
}