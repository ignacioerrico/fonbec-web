using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.NonPages.Review;

public partial class ScoreRating
{
    [Parameter, EditorRequired]
    public string Label { get; set; } = string.Empty;

    [Parameter]
    public int Value { get; set; }

    [Parameter]
    public EventCallback<int> ValueChanged { get; set; }

    /// <summary>Marks this criterion as the target of the numeric keyboard shortcuts.</summary>
    [Parameter]
    public bool Active { get; set; }

    [Parameter]
    public EventCallback OnActivate { get; set; }

    [Parameter]
    public string? ShortcutHint { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    private int? _hoveredValue;

    private static readonly string[] ScoreLabels =
        ["Muy bajo", "Bajo", "Regular", "Bueno", "Excelente"];

    private int? DisplayedValue => _hoveredValue is > 0 ? _hoveredValue : Value > 0 ? Value : null;

    private string Caption => DisplayedValue is { } score
        ? $"{score} · {ScoreLabels[score - 1]}"
        : "Sin puntuar";

    private string CaptionCssClass => DisplayedValue is null
        ? "score-caption score-caption-unset"
        : "score-caption";

    private string RootCssClass => Active ? "score-card score-card-active" : "score-card";

    private async Task OnSelectedValueChanged(int value)
    {
        Value = value;
        await ValueChanged.InvokeAsync(value);
    }

    private void OnHoveredValueChanged(int? value) => _hoveredValue = value;

    private async Task ActivateAsync() => await OnActivate.InvokeAsync();
}