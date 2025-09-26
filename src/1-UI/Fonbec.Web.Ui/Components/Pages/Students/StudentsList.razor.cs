using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models.Students;
using Fonbec.Web.Logic.Models.Students.Input;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Students;

public partial class StudentsList : AuthenticationRequiredComponentBase
{
    private List<StudentsListViewModel> _viewModels = [];

    private string[] _allEducationLevels = [];
    private string[] _allFacilitators = [];

    private string _searchString = string.Empty;

    private bool _sortByLastName;

    [Inject]
    public IStudentService StudentService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        Loading = true;

        _viewModels = await StudentService.GetAllStudentsAsync();

        _allEducationLevels = Enum.GetValues<EducationLevel>()
            .Select(el => el.EnumToString())
            .ToArray();

        _allFacilitators = _viewModels.Select(vm => vm.FacilitatorFullName)
            .Distinct()
            .OrderBy(fn => fn)
            .ToArray();

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
        || (!string.IsNullOrWhiteSpace(viewModel.StundentNickName)
            && $"{viewModel.StundentNickName} {viewModel.StudentLastName}".ContainsIgnoringAccents(_searchString))
        || viewModel.FacilitatorFullName.ContainsIgnoringAccents(_searchString)
        || (!string.IsNullOrWhiteSpace(viewModel.StudentPhoneNumber)
            && viewModel.StudentPhoneNumber.ContainsIgnoringAccents(_searchString));

    private async Task CommittedItemChangesAsync(StudentsListViewModel viewModel)
    {
        var updateStudentInputModel = new UpdateStudentInputModel(
            viewModel.StudentId,
            viewModel.StudentFirstName,
            viewModel.StudentLastName,
            viewModel.StundentNickName,
            viewModel.StudentEmail,
            viewModel.StudentPhoneNumber,
            viewModel.StudentNotes,
            viewModel.StudentSecondarySchoolStartYear,
            viewModel.StudentUniversityStartYear,
            viewModel.FacilitatorId
        );

        var result = await StudentService.UpdateStudentAsync(updateStudentInputModel);
        if (!result.AnyAffectedRows)
        {
            Snackbar.Add("No se pudo actualizar el becario.", Severity.Error);
        }

        _viewModels.Single(vm => vm.StudentId == viewModel.StudentId).LastUpdatedOnUtc = DateTime.Now;
    }

    private string StudentFullName(StudentsListViewModel viewModel) =>
        _sortByLastName
            ? $"{viewModel.StudentLastName}, {viewModel.StudentFirstName}"
            : $"{viewModel.StudentFirstName} {viewModel.StudentLastName}";
}