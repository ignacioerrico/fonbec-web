using Fonbec.Web.DataAccess.Entities.Enums;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.NonPages.Selectors;

public partial class EducationLevelSelector
{
    private readonly int _currentYear = DateTime.Now.Year;

    private EducationLevel _selectedLevel = EducationLevel.PrimarySchool;

    private bool _isSecondarySchoolStartYearKnown;

    private bool _isUniversityStartYearKnown;

    [Parameter]
    public DateTime? SecondarySchoolStartYear { get; set; }

    [Parameter]
    public EventCallback<DateTime?> SecondarySchoolStartYearChanged { get; set; }

    [Parameter]
    public DateTime? UniversityStartYear { get; set; }

    [Parameter]
    public EventCallback<DateTime?> UniversityStartYearChanged { get; set; }

    [Inject]
    public ISnackbar Snackbar { get; set; } = null!;

    protected override void OnInitialized()
    {
        if (SecondarySchoolStartYear.HasValue && UniversityStartYear.HasValue)
        {
            if (UniversityStartYear.Value < DateTime.Now)
            {
                _selectedLevel = EducationLevel.University;
            }
            else if (SecondarySchoolStartYear.Value < DateTime.Now)
            {
                _selectedLevel = EducationLevel.SecondarySchool;
            }

        }
        else if (UniversityStartYear.HasValue)
        {
            _selectedLevel = UniversityStartYear.Value < DateTime.Now
                ? EducationLevel.University
                : EducationLevel.SecondarySchool;
        }
        else if (SecondarySchoolStartYear.HasValue && SecondarySchoolStartYear.Value < DateTime.Now)
        {
            _selectedLevel = EducationLevel.SecondarySchool;
        }

        _isSecondarySchoolStartYearKnown = SecondarySchoolStartYear.HasValue;
        
        _isUniversityStartYearKnown = UniversityStartYear.HasValue;

        base.OnInitialized();
    }

    private async Task OnSelectedLevelChanged(EducationLevel selectedLevel)
    {
        _selectedLevel = selectedLevel;

        await OnIsSecondarySchoolStartYearKnownChanged(false);

        if (selectedLevel == EducationLevel.SecondarySchool)
        {
            // If the user selects Secondary school, set secondary school start year to last year (the student already started secondary school)
            SecondarySchoolStartYear = new DateTime(DateTime.Now.AddYears(-1).Year, 1, 1);
            await SecondarySchoolStartYearChanged.InvokeAsync(SecondarySchoolStartYear);
        }
        else if (selectedLevel == EducationLevel.University)
        {
            // If the user selects University, set university start year to last year (the student already started university)
            UniversityStartYear = new DateTime(DateTime.Now.AddYears(-1).Year, 1, 1);
            await UniversityStartYearChanged.InvokeAsync(UniversityStartYear);
        }
    }

    private async Task OnIsSecondarySchoolStartYearKnownChanged(bool isSecondarySchoolStartYearKnown)
    {
        _isSecondarySchoolStartYearKnown = isSecondarySchoolStartYearKnown;

        SecondarySchoolStartYear = isSecondarySchoolStartYearKnown
            ? new DateTime(DateTime.Now.Year, 1, 1)
            : null;

        await SecondarySchoolStartYearChanged.InvokeAsync(SecondarySchoolStartYear);

        if (!isSecondarySchoolStartYearKnown)
        {
            await OnIsUniversityStartYearKnownChanged(false);
        }
    }

    private async Task OnSecondarySchoolStartYearChanged(int? secondarySchoolStartYear)
    {
        if (secondarySchoolStartYear.HasValue && UniversityStartYear.HasValue && secondarySchoolStartYear.Value >= UniversityStartYear.Value.Year)
        {
            Snackbar.Add("La fecha de inicio del secundario no puede ser igual o posterior a la de la facultad.", Severity.Error);
            return;
        }

        SecondarySchoolStartYear = secondarySchoolStartYear.HasValue
            ? new DateTime(secondarySchoolStartYear.Value, 1, 1)
            : null;

        await SecondarySchoolStartYearChanged.InvokeAsync(SecondarySchoolStartYear);
    }

    private async Task OnIsUniversityStartYearKnownChanged(bool isUniversityStartYearKnown)
    {
        _isUniversityStartYearKnown = isUniversityStartYearKnown;

        UniversityStartYear = isUniversityStartYearKnown
            ? SecondarySchoolStartYear?.AddYears(1) ?? new DateTime(DateTime.Now.Year, 1, 1)
            : null;

        await UniversityStartYearChanged.InvokeAsync(UniversityStartYear);
    }

    private async Task OnUniversityStartYearChanged(int? universityStartYear)
    {
        if (universityStartYear.HasValue && SecondarySchoolStartYear.HasValue && universityStartYear.Value <= SecondarySchoolStartYear.Value.Year)
        {
            Snackbar.Add("La fecha de inicio de la facultad no puede ser igual o anterior a la del secundario.", Severity.Error);
            return;
        }

        UniversityStartYear = universityStartYear.HasValue
            ? new DateTime(universityStartYear.Value, 1, 1)
            : null;

        await UniversityStartYearChanged.InvokeAsync(UniversityStartYear);
    }
}