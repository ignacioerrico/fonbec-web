using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models.Facilitators;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.Pages.Facilitators;

[PageMetadata(nameof(FacilitatorStudentsList), "Mis becarios", [FonbecRole.Uploader])]
public partial class FacilitatorStudentsList : AuthenticationRequiredComponentBase
{
    private StudentsDashboardViewModel _dashboard = new();

    private string _searchString = string.Empty;
    private bool _sortByLastName;

    // Bound to the "Solo carta pendiente o rechazada" toggle. The filtering logic itself
    // lives in the Carta column (US 107); the shell only exposes the toggle state.
    private bool _letterFilterActive;

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
            ? $"Carta {label}"
            : "Carta";

    private bool FilterStudents(FacilitatorStudentsListViewModel viewModel) =>
        string.IsNullOrWhiteSpace(_searchString)
        || $"{viewModel.StudentFirstName} {viewModel.StudentLastName}".ContainsIgnoringAccents(_searchString)
        || (!string.IsNullOrEmpty(viewModel.StudentNickName)
            && $"{viewModel.StudentNickName} {viewModel.StudentLastName}".ContainsIgnoringAccents(_searchString));

    private string StudentFullName(FacilitatorStudentsListViewModel viewModel) =>
        _sortByLastName
            ? $"{viewModel.StudentLastName}, {viewModel.StudentFirstName}"
            : $"{viewModel.StudentFirstName} {viewModel.StudentLastName}";

    private static string ReportCardUploadLabel(FacilitatorStudentsListViewModel viewModel) =>
        viewModel.EducationLevel == EducationLevel.University
            ? "Subir libreta universitaria"
            : "Subir boletín";
}