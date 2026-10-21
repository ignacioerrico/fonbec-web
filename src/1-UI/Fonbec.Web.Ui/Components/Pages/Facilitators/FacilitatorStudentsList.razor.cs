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

    private bool _onlyMissingOrRejectedLetters;

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
            ? $"Carta de {label}"
            : "Carta";

    private bool FilterStudents(FacilitatorStudentsListViewModel viewModel) =>
        MatchesSearch(viewModel) && MatchesLetterFilter(viewModel);

    private bool MatchesSearch(FacilitatorStudentsListViewModel viewModel) =>
        string.IsNullOrWhiteSpace(_searchString)
        || $"{viewModel.StudentFirstName} {viewModel.StudentLastName}".ContainsIgnoringAccents(_searchString)
        || (!string.IsNullOrEmpty(viewModel.StudentNickName)
            && $"{viewModel.StudentNickName} {viewModel.StudentLastName}".ContainsIgnoringAccents(_searchString));

    private bool MatchesLetterFilter(FacilitatorStudentsListViewModel viewModel) =>
        LetterAggregation.MatchesLetterFilter(viewModel.LetterAggregate, _onlyMissingOrRejectedLetters);

    private string StudentFullName(FacilitatorStudentsListViewModel viewModel) =>
        _sortByLastName
            ? $"{viewModel.StudentLastName}, {viewModel.StudentFirstName}"
            : $"{viewModel.StudentFirstName} {viewModel.StudentLastName}";

    private static string ReportCardUploadLabel(FacilitatorStudentsListViewModel viewModel) =>
        viewModel.EducationLevel == EducationLevel.University
            ? "Subir libreta universitaria"
            : "Subir boletín";

    private static string ReportCardMissingTooltip(FacilitatorStudentsListViewModel viewModel) =>
        viewModel.EducationLevel == EducationLevel.University
            ? "Aún no hay libreta universitaria"
            : "Aún no hay boletín";

    private static List<SponsorLetterStatusViewModel> RejectedLetters(FacilitatorStudentsListViewModel viewModel) =>
        viewModel.LetterStatuses.Where(letter => letter.Status == LetterSlotStatus.Rejected).ToList();

    private static string RejectedLetterText(SponsorLetterStatusViewModel letter, bool showRecipient)
    {
        var reason = string.IsNullOrWhiteSpace(letter.RejectionReason)
            ? "Rechazada"
            : $"Rechazada: {letter.RejectionReason}";
        return showRecipient ? $"{letter.RecipientName} — {reason}" : reason;
    }

    private static string ReportChipStatusText(ReportCardChipViewModel report) =>
        report.Status == DocumentStatus.Rejected && !string.IsNullOrWhiteSpace(report.RejectionReason)
            ? $"Rechazada: {report.RejectionReason}"
            : report.Status.Label();

    private string LetterUploadUrl(FacilitatorStudentsListViewModel viewModel, SponsorLetterStatusViewModel sponsor) =>
        NavRoutes.FacilitatorUploadLetter(
            viewModel.StudentId, _dashboard.CurrentPlanId!.Value, sponsor.SponsorId, sponsor.CompanyId);

    private bool ShowLetterUpload(FacilitatorStudentsListViewModel viewModel) =>
        _dashboard.CurrentPlanId is not null
        && !viewModel.IsLetterExemptForCurrentPlan
        && viewModel.SponsorsNeedingLetter.Count > 0;

    private static string LetterDetailsAriaLabel(FacilitatorStudentsListViewModel viewModel) =>
        $"Ver detalle de la carta: {viewModel.LetterAggregate.Label()}";

    private static string ReportCardHistoryLabel(FacilitatorStudentsListViewModel viewModel) =>
        viewModel.EducationLevel == EducationLevel.University
            ? "Ver historial de libretas universitarias"
            : "Ver historial de boletines";

    private async Task ShowLetterDetailsAsync(FacilitatorStudentsListViewModel student)
    {
        var parameters = new DialogParameters<CurrentLetterDetailsDialog>
        {
            { dialog => dialog.Student, student },
        };

        var title = _dashboard.CurrentPlanLabel is { } label
            ? $"Carta de {label} — {StudentFullName(student)}"
            : $"Carta — {StudentFullName(student)}";

        await DialogService.ShowAsync<CurrentLetterDetailsDialog>(title, parameters);
    }
}