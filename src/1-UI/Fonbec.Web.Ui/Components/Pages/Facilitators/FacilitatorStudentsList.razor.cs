using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models.Facilitators;
using Fonbec.Web.Logic.Models.Students;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Components.NonPages.Dialogs;
using Fonbec.Web.Ui.Constants;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Facilitators;

[PageMetadata(nameof(FacilitatorStudentsList), "Mis becarios", [FonbecRole.Uploader])]
public partial class FacilitatorStudentsList : AuthenticationRequiredComponentBase
{
    private StudentsDashboardViewModel _dashboard = new();

    private string _searchString = string.Empty;
    private bool _sortByLastName;

    // "Solo carta pendiente o rechazada" toggle (us111).
    private bool _letterFilterActive;

    [Inject]
    public IFacilitatorService FacilitatorService { get; set; } = null!;

    [Inject]
    public IDialogService DialogService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        Loading = true;
        _dashboard = await FacilitatorService.GetStudentsDashboardAsync(FonbecClaim.UserId);
        Loading = false;
    }

    private string LetterColumnTitle =>
        _dashboard.CurrentPlanLabel is { } label
            ? $"Carta {label}"
            : "Carta";

    private bool FilterStudents(FacilitatorStudentsListViewModel viewModel) =>
        MatchesSearch(viewModel) && MatchesLetterFilter(viewModel);

    private bool MatchesSearch(FacilitatorStudentsListViewModel viewModel) =>
        string.IsNullOrWhiteSpace(_searchString)
        || $"{viewModel.StudentFirstName} {viewModel.StudentLastName}".ContainsIgnoringAccents(_searchString)
        || (!string.IsNullOrEmpty(viewModel.StudentNickName)
            && $"{viewModel.StudentNickName} {viewModel.StudentLastName}".ContainsIgnoringAccents(_searchString));

    private bool MatchesLetterFilter(FacilitatorStudentsListViewModel viewModel) =>
        LetterAggregation.MatchesLetterFilter(viewModel.LetterAggregate, _letterFilterActive);

    private string StudentFullName(FacilitatorStudentsListViewModel viewModel) =>
        _sortByLastName
            ? $"{viewModel.StudentLastName}, {viewModel.StudentFirstName}"
            : $"{viewModel.StudentFirstName} {viewModel.StudentLastName}";

    private static string ReportCardUploadLabel(FacilitatorStudentsListViewModel viewModel) =>
        viewModel.EducationLevel == EducationLevel.University
            ? "Subir libreta universitaria"
            : "Subir boletín";

    // Letters can be addressed to either an individual sponsor or a company (each sponsorship
    // resolves to exactly one recipient), so all of a student's required slots are letter recipients.
    private static List<SponsorLetterStatusViewModel> LetterSponsors(FacilitatorStudentsListViewModel viewModel) =>
        viewModel.LetterStatuses;

    private string LetterUploadUrl(FacilitatorStudentsListViewModel viewModel, SponsorLetterStatusViewModel sponsor) =>
        NavRoutes.FacilitatorUploadLetter(
            viewModel.StudentId, _dashboard.CurrentPlanId!.Value, sponsor.SponsorId, sponsor.CompanyId);

    private async Task OpenLetterSponsorPickerAsync(FacilitatorStudentsListViewModel viewModel)
    {
        var parameters = new DialogParameters<LetterSponsorPickerDialog>
        {
            { d => d.StudentId, viewModel.StudentId },
            { d => d.StudentFullName, StudentFullName(viewModel) },
            { d => d.PlanId, _dashboard.CurrentPlanId!.Value },
            { d => d.PlanLabel, _dashboard.CurrentPlanLabel ?? string.Empty },
            { d => d.Sponsors, LetterSponsors(viewModel) },
        };

        await DialogService.ShowAsync<LetterSponsorPickerDialog>("Subir carta", parameters);
    }
}