using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models.Facilitators;
using Fonbec.Web.Logic.Models.Students;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Constants;
using Microsoft.AspNetCore.Components;

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

    private string LetterUploadUrl(FacilitatorStudentsListViewModel viewModel, SponsorLetterStatusViewModel sponsor) =>
        NavRoutes.FacilitatorUploadLetter(
            viewModel.StudentId, _dashboard.CurrentPlanId!.Value, sponsor.SponsorId, sponsor.CompanyId);

    // Detail "Carta" text for students with no letter slots to list (exempt or no active plan).
    private static string LetterDetailText(FacilitatorStudentsListViewModel viewModel) =>
        viewModel.LetterAggregate == LetterAggregateStatus.Exempt
            ? string.IsNullOrWhiteSpace(viewModel.LetterExemptionReason)
                ? "Eximido"
                : $"Eximido: {viewModel.LetterExemptionReason}"
            : "Sin campaña activa";
}