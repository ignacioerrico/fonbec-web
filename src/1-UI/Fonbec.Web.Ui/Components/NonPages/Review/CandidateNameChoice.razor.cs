using Fonbec.Web.Logic.Models.Review;
using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.NonPages.Review;

public partial class CandidateNameChoice
{
    [Parameter]
    public string Label { get; set; } = string.Empty;

    [Parameter]
    public IReadOnlyList<CandidateNameViewModel> Names { get; set; } = [];

    [Parameter]
    public CandidateNameKey CorrectKey { get; set; }

    [Parameter]
    public string NoneOfTheAboveLabel { get; set; } = "Ninguno de los anteriores";

    [Parameter]
    public string NotIndicatedLabel { get; set; } = "No indicado";

    [Parameter]
    public CandidateNameSelection Selection { get; set; }

    [Parameter]
    public EventCallback<CandidateNamePick> SelectionChanged { get; set; }

    /// <summary>Raised when the name is present but is none of the listed candidates.</summary>
    [Parameter]
    public EventCallback OnNoneOfTheAbove { get; set; }

    /// <summary>Raised when the document does not state the name at all.</summary>
    [Parameter]
    public EventCallback OnNotIndicated { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    private CandidateNameKey? _selectedKey;
    private bool _hasUserSelected;

    protected override void OnParametersSet()
    {
        if (_hasUserSelected)
        {
            return;
        }

        _selectedKey = Selection == CandidateNameSelection.Correct ? CorrectKey : null;
    }

    private async Task OnSelectedKeyChanged(CandidateNameKey? value)
    {
        _hasUserSelected = true;
        _selectedKey = value;

        var selection = value switch
        {
            null => CandidateNameSelection.None,
            _ when value.Value == CorrectKey => CandidateNameSelection.Correct,
            _ => CandidateNameSelection.Wrong,
        };

        var displayName = value is { } key
            ? Names.FirstOrDefault(n => n.Key == key)?.DisplayName
            : null;

        await SelectionChanged.InvokeAsync(new CandidateNamePick(selection, displayName));
    }
}
