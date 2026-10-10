using System.Globalization;
using Fonbec.Web.Logic.Util;
using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.NonPages;

public partial class RelatedCountFilter
{
    private const int MaxPosition = (int)RelatedCount.Any;

    private string[] _tickLabels = [];

    [Parameter]
    public string? Class { get; set; }

    /// <summary>
    /// Heading above the slider, such as "Padrinos" or "Becarios".
    /// </summary>
    [Parameter, EditorRequired]
    public string Caption { get; set; } = null!;

    /// <summary>
    /// Singular noun used in the first stop, such as "padrino" or "becario".
    /// </summary>
    [Parameter, EditorRequired]
    public string Noun { get; set; } = null!;

    [Parameter]
    public string? Hint { get; set; }

    [Parameter]
    public RelatedCount Value { get; set; } = RelatedCount.Any;

    [Parameter]
    public EventCallback<RelatedCount> ValueChanged { get; set; }

    private string SliderAriaLabel => $"Cantidad de {Noun}s";

    private string SelectedLabel =>
        _tickLabels.Length == 0 ? string.Empty : _tickLabels[Math.Clamp((int)Value, 0, _tickLabels.Length - 1)];

    protected override void OnParametersSet()
    {
        _tickLabels =
        [
            $"Ningún {Noun}",
            "Uno solo",
            "Solo dos o más",
            "Cualquier cantidad",
        ];
    }

    private string LabelClass(RelatedCount filter) =>
        filter == Value
            ? "related-count-filter__label related-count-filter__label--selected"
            : "related-count-filter__label";

    private static string LabelStyle(int index) =>
        string.Create(CultureInfo.InvariantCulture, $"left: {index * 100d / MaxPosition:0.##}%");

    private Task OnSliderChanged(int value)
    {
        if (!Enum.IsDefined(typeof(RelatedCount), value))
        {
            return Task.CompletedTask;
        }

        return SelectAsync((RelatedCount)value);
    }

    private Task SelectAsync(RelatedCount filter) =>
        ValueChanged.InvokeAsync(filter);
}