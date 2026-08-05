using Fonbec.Web.Logic.Models.Students;
using Fonbec.Web.Ui.Constants;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.NonPages.Dialogs;

public partial class LetterSponsorPickerDialog
{
    private int? _selectedSponsorshipId;

    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public int StudentId { get; set; }

    [Parameter]
    [EditorRequired]
    public string StudentFullName { get; set; } = string.Empty;

    [Parameter]
    [EditorRequired]
    public int PlanId { get; set; }

    [Parameter]
    [EditorRequired]
    public string PlanLabel { get; set; } = string.Empty;

    [Parameter]
    [EditorRequired]
    public List<SponsorLetterStatusViewModel> Sponsors { get; set; } = [];

    [Inject]
    public NavigationManager NavigationManager { get; set; } = null!;

    private void Continue()
    {
        var sponsor = Sponsors.SingleOrDefault(s => s.SponsorshipId == _selectedSponsorshipId);
        if (sponsor is null)
        {
            return;
        }

        var url = NavRoutes.FacilitatorUploadLetter(StudentId, PlanId, sponsor.SponsorId, sponsor.CompanyId);
        NavigationManager.NavigateTo(url);
        MudDialog.Close();
    }

    private void Cancel() => MudDialog.Cancel();
}
