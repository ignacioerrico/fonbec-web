using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.DataModels.Students;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.Models.Students;
using Fonbec.Web.Logic.Models.Students.Input;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;


namespace Fonbec.Web.Ui.Components.Pages.Students;

[PageMetadata(nameof(StudentsList), "Lista de becarios", [FonbecRole.Manager])]
public partial class StudentsList : AuthenticationRequiredComponentBase
{
    private List<StudentsListViewModel> _viewModels = new();
    private List<StudentsListViewModel> _allStudents = new();

    private bool _sortByLastName;
    private string _searchString = string.Empty;

    // Multi-select values for facilitators
    private IEnumerable<string> _selectedFacilitatorIds = new HashSet<string>();
    private EducationLevel? _selectedEducationLevel;

    [Inject]
    public IStudentService StudentService { get; set; } = null!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _allStudents = await StudentService.GetAllStudentsAsync();
            await ApplyFilters();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error loading students: {ex.Message}", Severity.Error);
        }
    }

    private async Task OnFacilitatorSelectionChanged()
    {
        await ApplyFilters();
    }

    // Helper to display facilitator names in the select
    private string GetFacilitatorName(string facilitatorId)
    {
        if (string.IsNullOrEmpty(facilitatorId))
            return string.Empty;

        if (int.TryParse(facilitatorId, out var id))
        {
            var facilitator = _allStudents?.FirstOrDefault(f => f.FacilitatorId == id);
            return facilitator?.FacilitatorFullName ?? string.Empty;
        }

        return string.Empty;
    }

    // Also, modify your ApplyFilters method to handle null/empty selections better:
    private async Task ApplyFilters()
    {
        Loading = true;

        try
        {
            // Safely parse facilitator IDs
            var facilitatorIds = new List<int>();
            if (_selectedFacilitatorIds != null)
            {
                foreach (var id in _selectedFacilitatorIds)
                {
                    if (!string.IsNullOrWhiteSpace(id) && int.TryParse(id.Trim(), out var parsedId))
                    {
                        facilitatorIds.Add(parsedId);
                    }
                }
            }

            var filter = new StudentsFilterModel
            {
                FacilitatorIds = facilitatorIds,
                EducationLevel = _selectedEducationLevel,
                Search = _searchString?.Trim() ?? string.Empty
            };

            var students = await StudentService.GetStudentsAsync(filter);

            _viewModels = students ?? new List<StudentsListViewModel>();
        }
        catch (Exception ex)
        {
            _viewModels = _allStudents ?? new List<StudentsListViewModel>();
            Snackbar.Add($"Error applying filters: {ex.Message}", Severity.Error);
            Console.WriteLine($"Error applying filters: {ex.Message}");
        }
        finally
        {
            Loading = false;
            await InvokeAsync(StateHasChanged);
        }
    }
    private async Task ClearFilters()
    {
        _searchString = string.Empty;
        _selectedEducationLevel = null;

        var emptySelection = new HashSet<string>();

        _selectedFacilitatorIds = emptySelection;

        await InvokeAsync(StateHasChanged);

        await Task.Delay(1);

        await ApplyFilters();
    }

    private async Task CommittedItemChangesAsync(StudentsListViewModel viewModel)
    {
        try
        {
            var input = new UpdateStudentInputModel(
                viewModel.StudentId,
                viewModel.StudentFirstName,
                viewModel.StudentLastName,
                viewModel.StundentNickName,
                viewModel.StudentEmail,
                viewModel.StudentPhoneNumber,
                viewModel.StudentNotes,
                viewModel.StudentSecondarySchoolStartYear,
                viewModel.StudentUniversityStartYear,
                viewModel.FacilitatorId,
                FonbecClaim.UserId
            );

            var result = await StudentService.UpdateStudentAsync(input);

            if (!result.AnyAffectedRows)
            {
                Snackbar.Add("No se pudo actualizar el becario.", Severity.Error);
            }
            else
            {
                Snackbar.Add("Becario actualizado correctamente.", Severity.Success);

                var updatedVm = _viewModels.SingleOrDefault(vm => vm.StudentId == viewModel.StudentId);
                if (updatedVm != null)
                {
                    updatedVm.LastUpdatedOnUtc = DateTime.UtcNow;
                }
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Error updating student: {ex.Message}", Severity.Error);
            Console.WriteLine($"Error updating student: {ex.Message}");
        }
    }

    // Helper for unique facilitators
    private IEnumerable<StudentsListViewModel> UniqueFacilitators()
    {
        if (_allStudents == null || !_allStudents.Any())
            return Enumerable.Empty<StudentsListViewModel>();

        return _allStudents
            .Where(s => s.FacilitatorId > 0)
            .GroupBy(s => new { s.FacilitatorId, s.FacilitatorFullName })
            .Select(g => g.First())
            .OrderBy(f => f.FacilitatorFullName);
    }
}

