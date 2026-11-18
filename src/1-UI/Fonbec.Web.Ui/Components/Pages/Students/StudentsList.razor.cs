using System.Security.Cryptography;
using System.Text.Json;
using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models.Sponsorships;
using Fonbec.Web.Logic.Models.Students;
using Fonbec.Web.Logic.Models.Students.Input;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Logic.Util;
using Fonbec.Web.Ui.Components.NonPages.Dialogs;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.JSInterop;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Students;

[PageMetadata(nameof(StudentsList), "Lista de becarios", [FonbecRole.Admin, FonbecRole.Manager])]
public partial class StudentsList : AuthenticationRequiredComponentBase
{
    private List<StudentsListViewModel> _viewModels = [];

    private StudentsListViewModel _originalViewModel = new();

    private IEnumerable<string> _allEducationLevels = [];

    private IEnumerable<string> _allReportCardAssessments = [];

    private IEnumerable<string> _allFacilitators = [];

    private IEnumerable<string> _allChapters = [];

    private string _searchString = string.Empty;

    private bool _fullView = true;

    private bool _sortByLastName;

    private RelatedCount _sponsorCountFilter = RelatedCount.Any;

    private bool _restoringFilters;
    private bool _filtersLoaded;
    private string[]? _facilitatorFilter;
    private string[]? _educationFilter;
    private string[]? _reportCardFilter;
    private string[]? _chapterFilter;
    private StudentColumnFilter[] _columnFilters = [];

    private string FilterStorageKey => $"students-filters:v1:{FonbecClaim.UserId}:{FonbecClaim.ChapterId}";

    private bool ShowLetterAssessments => _viewModels.Any(vm => vm.HasLetterAssessment);

    [Inject]
    public IStudentService StudentService { get; set; } = null!;

    [Inject]
    public IDialogService DialogService { get; set; } = null!;

    [Inject]
    private ProtectedSessionStorage FilterStorage { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        Loading = true;
        await base.OnInitializedAsync();


        _viewModels = await StudentService.GetAllStudentsAsync(FonbecClaim.ChapterId);

        _allEducationLevels = Enum.GetValues<EducationLevel>()
            .Select(el => el.EnumToString());

        _allReportCardAssessments = Enum.GetValues<ReportCardAssessment>()
            .Select(assessment => assessment.EnumToString())
            .Append(StudentsListViewModel.NoReportCardAssessmentLabel);

        _allFacilitators = _viewModels.Select(vm => vm.FacilitatorFullName)
            .Distinct()
            .OrderBy(fn => fn);

        _allChapters = _viewModels.Select(vm => vm.StudentChapterName)
            .Distinct()
            .OrderBy(cn => cn);

        Loading = false;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Loading || _restoringFilters || _filtersLoaded)
        {
            return;
        }

        _restoringFilters = true;
        try
        {
            var stored = await FilterStorage.GetAsync<StudentListFilters>(FilterStorageKey);
            if (stored.Success && stored.Value is { } filters)
            {
                _searchString = filters.Search ?? string.Empty;
                _sponsorCountFilter = Enum.IsDefined(filters.SponsorCount) ? filters.SponsorCount : RelatedCount.Any;
                _facilitatorFilter = AvailableSelection(filters.Facilitators, _allFacilitators);
                _educationFilter = AvailableSelection(filters.EducationLevels, _allEducationLevels);
                _reportCardFilter = AvailableSelection(filters.ReportCards, _allReportCardAssessments);
                _chapterFilter = AvailableSelection(filters.Chapters, _allChapters);
                _columnFilters = filters.Columns ?? [];
            }
        }
        catch (Exception exception) when (exception is JSException or JSDisconnectedException or CryptographicException or JsonException)
        {
            // Storage may be unavailable or contain data from a previous data protection key.
        }

        _filtersLoaded = true;
        StateHasChanged();
    }

    private static string[]? AvailableSelection(string[]? selected, IEnumerable<string> available)
    {
        if (selected is not { Length: > 0 })
        {
            return selected;
        }

        var remaining = selected.Intersect(available).ToArray();
        return remaining.Length == 0 ? null : remaining;
    }

    private Task SaveFacilitatorFilterAsync(string[]? selected)
    {
        _facilitatorFilter = selected;
        return SaveFiltersAsync();
    }

    private Task SaveEducationFilterAsync(string[]? selected)
    {
        _educationFilter = selected;
        return SaveFiltersAsync();
    }

    private Task SaveReportCardFilterAsync(string[]? selected)
    {
        _reportCardFilter = selected;
        return SaveFiltersAsync();
    }

    private Task SaveChapterFilterAsync(string[]? selected)
    {
        _chapterFilter = selected;
        return SaveFiltersAsync();
    }

    private Task SaveColumnFiltersAsync(StudentColumnFilter[] filters)
    {
        _columnFilters = filters;
        return SaveFiltersAsync();
    }

    private async Task SaveFiltersAsync()
    {
        var filters = new StudentListFilters(
            _searchString, _sponsorCountFilter, _facilitatorFilter, _educationFilter,
            _reportCardFilter, _chapterFilter, _columnFilters);

        try
        {
            if (filters.IsEmpty)
            {
                await FilterStorage.DeleteAsync(FilterStorageKey);
            }
            else
            {
                await FilterStorage.SetAsync(FilterStorageKey, filters);
            }
        }
        catch (Exception exception) when (exception is JSException or JSDisconnectedException)
        {
            // Keep filtering the current grid when browser storage cannot be written.
        }
    }

    /// <summary>
    /// Called by MudTable to determine if a row should be displayed
    /// </summary>
    /// <param name="viewModel">The student view model to evaluate against the search string. Cannot be null.</param>
    /// <returns>true if the student matches the search string in any of the relevant fields; otherwise, false.</returns>
    private bool Filter(StudentsListViewModel viewModel) =>
        _sponsorCountFilter.Matches(ActiveOrUpcomingSponsorCount(viewModel))
        && (_facilitatorFilter?.Contains(viewModel.FacilitatorFullName) ?? true)
        && (_educationFilter?.Contains(viewModel.StudentCurrentEducationLevel) ?? true)
        && (_reportCardFilter?.Contains(viewModel.LatestReportCardAssessmentLabel) ?? true)
        && (_chapterFilter?.Contains(viewModel.StudentChapterName) ?? true)
        && (string.IsNullOrWhiteSpace(_searchString)
            || $"{viewModel.StudentFirstName} {viewModel.StudentLastName}".ContainsIgnoringAccents(_searchString)
            || (!string.IsNullOrEmpty(viewModel.StudentNickName)
                && $"{viewModel.StudentNickName} {viewModel.StudentLastName}".ContainsIgnoringAccents(_searchString))
            || viewModel.FacilitatorFullName.ContainsIgnoringAccents(_searchString)
            || viewModel.ActiveSponsors.Any(s => s.Name.ContainsIgnoringAccents(_searchString))
            || (!string.IsNullOrEmpty(viewModel.StudentEmail)
                && viewModel.StudentEmail.Contains(_searchString, StringComparison.OrdinalIgnoreCase))
            || (!string.IsNullOrEmpty(viewModel.StudentPhoneNumber)
                && viewModel.StudentPhoneNumber.ContainsIgnoringSpaces(_searchString)));

    private static int ActiveOrUpcomingSponsorCount(StudentsListViewModel viewModel) =>
        viewModel.ActiveSponsors.Count(sponsor => sponsor.TimelineStatus != SponsorshipTimelineStatus.Finished);

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

        try
        {
            var result = await StudentService.UpdateStudentAsync(updateStudentInputModel);
            if (result.AnyAffectedRows)
            {
                // Update timestamp in UI
                _viewModels.Single(vm => vm.StudentId == modifiedViewModel.StudentId).LastUpdatedOnUtc = DateTime.Now;
                Snackbar.Add("Becario actualizado correctamente.", Severity.Success);
            }
            else
            {
                Snackbar.Add("No se pudo actualizar el becario.", Severity.Error);
                RevertItemChanges(modifiedViewModel.StudentId);
            }
        }
        catch (InvalidOperationException exception)
        {
            Snackbar.Add(exception.Message, Severity.Error);
            RevertItemChanges(modifiedViewModel.StudentId);
        }
        finally
        {
            Loading = false;
        }
    }

    private void RevertItemChanges(int studentId)
    {
        var index = _viewModels.FindIndex(vm => vm.StudentId == studentId);
        if (index >= 0)
        {
            _viewModels[index] = _originalViewModel.DeepClone();
        }
    }

    private static List<ChartSeries> AssessmentSeries(LetterAssessmentSummaryViewModel assessment) =>
    [
        new() { Name = "Ortografía", Data = assessment.SpellingByPlan },
        new() { Name = "Legibilidad", Data = assessment.PenmanshipByPlan },
        new() { Name = "Contenido", Data = assessment.ContentByPlan },
    ];

    private static string ReportCardAssessmentTooltip(LatestReportCardAssessmentViewModel assessment)
    {
        var period = assessment.Period.ToDateTime(TimeOnly.MinValue).ToSpanishMonthYear();
        return string.IsNullOrWhiteSpace(assessment.Description)
            ? period
            : $"{period} — {assessment.Description}";
    }

    private string StudentFullName(StudentsListViewModel viewModel) =>
        _sortByLastName
            ? $"{viewModel.StudentLastName}, {viewModel.StudentFirstName}"
            : $"{viewModel.StudentFirstName} {viewModel.StudentLastName}";

    private async Task OpenUploadDialogAsync(StudentsListViewModel viewModel)
    {
        if (FonbecClaim.ChapterId is not { } managerChapterId)
        {
            Snackbar.Add("No se puede subir el documento para este becario.", Severity.Error);
            return;
        }

        var parameters = new DialogParameters<ManagerUploadTypePickerDialog>
        {
            { d => d.StudentId, viewModel.StudentId },
            { d => d.StudentFullName, StudentFullName(viewModel) },
            { d => d.ManagerChapterId, managerChapterId },
        };

        await DialogService.ShowAsync<ManagerUploadTypePickerDialog>("Subir documento", parameters);
    }
}
