using Fonbec.Web.Logic.Models.Facilitators;
using Fonbec.Web.Logic.Models.Students;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.NonPages.Dialogs;

public partial class CurrentLetterDetailsDialog
{
    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public FacilitatorStudentsListViewModel Student { get; set; } = null!;

    private string LetterDetailText =>
        Student.LetterAggregate == LetterAggregateStatus.Exempt
            ? string.IsNullOrWhiteSpace(Student.LetterExemptionReason)
                ? "Eximido"
                : $"Eximido: {Student.LetterExemptionReason}"
            : "Sin campaña activa";
}