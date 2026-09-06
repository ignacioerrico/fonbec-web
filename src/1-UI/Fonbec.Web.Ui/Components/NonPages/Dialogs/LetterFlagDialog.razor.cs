using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.Models.LetterFollowUp;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.NonPages.Dialogs;

public partial class LetterFlagDialog
{
    private RedFlagPriority _priority;

    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter, EditorRequired]
    public LetterFollowUpTaskViewModel Task { get; set; } = null!;

    private string Title => Task.Kind == LetterFollowUpTaskKind.RedFlag
        ? "Bandera roja"
        : "Bandera verde";

    protected override void OnParametersSet()
    {
        _priority = Task.Priority ?? RedFlagPriority.Low;
    }

    private void Close() =>
        MudDialog.Close(new LetterFlagDialogResult(false, SelectedPriority()));

    private void Resolve() =>
        MudDialog.Close(new LetterFlagDialogResult(true, SelectedPriority()));

    private RedFlagPriority? SelectedPriority() =>
        Task.Kind == LetterFollowUpTaskKind.RedFlag ? _priority : null;
}

public sealed record LetterFlagDialogResult(
    bool Resolve,
    RedFlagPriority? Priority);
