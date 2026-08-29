using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.NonPages.Dialogs;

public partial class LetterExemptionReasonDialog
{
    private string _reason = string.Empty;

    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    public string Title { get; set; } = "Eximir de carta";

    [Parameter]
    public string Prompt { get; set; } = "Motivo de la exención (obligatorio)";

    [Parameter]
    public string? PlanLabel { get; set; }

    private void Cancel() => MudDialog.Cancel();

    private void Confirm() => MudDialog.Close(_reason.Trim());
}