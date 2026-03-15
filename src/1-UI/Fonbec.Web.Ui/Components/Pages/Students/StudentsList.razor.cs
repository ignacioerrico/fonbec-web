using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models.Students;
using Fonbec.Web.Logic.Models.Students.Input;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Students;

[PageMetadata(nameof(StudentsList), "Lista de becarios", [FonbecRole.Manager])]
public partial class StudentsList : AuthenticationRequiredComponentBase
{
    private List<StudentsListViewModel> _viewModels = [];

    private StudentsListViewModel _originalViewModel = new();

    private IEnumerable<string> _allEducationLevels = [];

    private IEnumerable<string> _allFacilitators = [];

    private string _searchString = string.Empty;

    private bool _sortByLastName;

    [Inject]
    public IStudentService StudentService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        Loading = true;

        _viewModels = await StudentService.GetAllStudentsAsync(FonbecClaim.ChapterId);

        _allEducationLevels = Enum.GetValues<EducationLevel>()
            .Select(el => el.EnumToString());

        _allFacilitators = _viewModels.Select(vm => vm.FacilitatorFullName)
            .Distinct()
            .OrderBy(fn => fn);

        Loading = false;
    }

    /// <summary>
    /// Called by MudTable to determine if a row should be displayed
    /// </summary>
    /// <param name="viewModel">The student view model to evaluate against the search string. Cannot be null.</param>
    /// <returns>true if the student matches the search string in any of the relevant fields; otherwise, false.</returns>
    private bool Filter(StudentsListViewModel viewModel) =>
        string.IsNullOrWhiteSpace(_searchString)
        || $"{viewModel.StudentFirstName} {viewModel.StudentLastName}".ContainsIgnoringAccents(_searchString)
        || (!string.IsNullOrEmpty(viewModel.StudentNickName)
            && $"{viewModel.StudentNickName} {viewModel.StudentLastName}".ContainsIgnoringAccents(_searchString))
        || viewModel.FacilitatorFullName.ContainsIgnoringAccents(_searchString)
        || (!string.IsNullOrEmpty(viewModel.StudentEmail)
            && viewModel.StudentEmail.Contains(_searchString, StringComparison.OrdinalIgnoreCase))
        || (!string.IsNullOrEmpty(viewModel.StudentPhoneNumber)
            && viewModel.StudentPhoneNumber.ContainsIgnoringSpaces(_searchString));

    private void StartedEditingItem(StudentsListViewModel originalViewModel) =>
        _originalViewModel = originalViewModel.DeepClone();

    private async Task CommittedItemChangesAsync(StudentsListViewModel modifiedViewModel)
    {
        if (_originalViewModel.IsEqualTo(modifiedViewModel))
        {
            Snackbar.Add("No se realizaron cambios.", Severity.Info);
            return;
        }

        var updateStudentInputModel = new UpdateStudentInputModel(
            modifiedViewModel.StudentId,
            modifiedViewModel.StudentFirstName,
            modifiedViewModel.StudentLastName,
            modifiedViewModel.StudentNickName,
            modifiedViewModel.StudentEmail,
            modifiedViewModel.StudentPhoneNumber,
            modifiedViewModel.Notes,
            modifiedViewModel.StudentSecondarySchoolStartYear,
            modifiedViewModel.StudentUniversityStartYear,
            modifiedViewModel.FacilitatorId,
            FonbecClaim.UserId
        );

        Loading = true;

        var result = await StudentService.UpdateStudentAsync(updateStudentInputModel);

        Loading = false;

        if (result.AnyAffectedRows)
        {
            // Update timestamp in UI
            _viewModels.Single(vm => vm.StudentId == modifiedViewModel.StudentId).LastUpdatedOnUtc = DateTime.Now;
            Snackbar.Add("Padrino actualizado correctamente.", Severity.Success);
        }
        else
        {
            Snackbar.Add("No se pudo actualizar el becario.", Severity.Error);
        }
    }

    private string StudentFullName(StudentsListViewModel viewModel) =>
        _sortByLastName
            ? $"{viewModel.StudentLastName}, {viewModel.StudentFirstName}"
            : $"{viewModel.StudentFirstName} {viewModel.StudentLastName}";
}