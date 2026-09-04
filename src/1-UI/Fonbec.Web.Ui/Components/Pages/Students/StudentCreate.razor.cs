using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.Models.Students.Input;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Constants;
using Fonbec.Web.Ui.Models.Student;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Students;

[PageMetadata(nameof(StudentCreate), "Crear y actualizar becario", [FonbecRole.Admin, FonbecRole.Manager])]
public partial class StudentCreate : AuthenticationRequiredComponentBase
{
    private readonly StudentCreateBindModel _bindModel = new();

    private int EffectiveChapterId => FonbecClaim.ChapterId ?? _bindModel.ChapterId;
    private bool IsChapterKnown => EffectiveChapterId > 0;
    private bool FacilitatorsLoadedForCurrentChapter =>
        _facilitatorsLoadedChapterId == EffectiveChapterId;
    private bool FacilitatorsAvailableForCurrentChapter =>
        FacilitatorsLoadedForCurrentChapter && _anyFacilitators;
    private bool NoFacilitatorsForCurrentChapter =>
        IsChapterKnown && FacilitatorsLoadedForCurrentChapter && !_anyFacilitators;
    private bool IsFormDisabled => !_anyChapters
                                   || (FonbecClaim.ChapterId.HasValue
                                       && !FacilitatorsAvailableForCurrentChapter);
    private bool _anyChapters;
    private bool _anyFacilitators;
    private int _facilitatorsLoadedChapterId;

    private bool _formValidationSucceeded;

    private bool _saving;

    private bool SaveButtonDisabled => Loading
                                       || _saving
                                       || IsFormDisabled
                                       || !IsChapterKnown
                                       || !FacilitatorsAvailableForCurrentChapter
                                       || !_formValidationSucceeded;

    [Inject]
    public IStudentService StudentService { get; set; } = null!;

    private async Task OnChaptersLoaded(int chaptersCount) =>
        _anyChapters = chaptersCount > 0;

    private async Task OnFacilitatorsLoaded(int totalFacilitators)
    {
        _anyFacilitators = totalFacilitators > 0;
        _facilitatorsLoadedChapterId = EffectiveChapterId;
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        // Managers have a fixed chapter — pre-set it so the form isn't disabled
        if (FonbecClaim.ChapterId.HasValue)
        {
            _anyChapters = true;
            _bindModel.ChapterId = FonbecClaim.ChapterId.Value;
        }
    }

    private async Task Save()
    {
        if (FonbecClaim.ChapterId is null)
        {
            if (_bindModel.ChapterId == 0)
            {
                Snackbar.Add("La filial no es válida.", Severity.Error);
                return;
            }
        }
        else
        {
            _bindModel.ChapterId = FonbecClaim.ChapterId.Value;
        }

        if (_bindModel.FacilitatorId == 0)
        {
            Snackbar.Add("El mediador no es válido.", Severity.Error);
            return;
        }

        _saving = true;

        var createStudentInputModel = new CreateStudentInputModel(
            _bindModel.ChapterId,
            _bindModel.StudentFirstName,
            _bindModel.StudentLastName,
            _bindModel.StudentNickName,
            _bindModel.StudentGender,
            _bindModel.StudentEmail,
            _bindModel.StudentPhoneNumber,
            _bindModel.StudentNotes,
            _bindModel.StudentSecondarySchoolStartYear,
            _bindModel.StudentUniversityStartYear,
            _bindModel.FacilitatorId,
            FonbecClaim.UserId);

        try
        {
            var result = await StudentService.CreateStudentAsync(createStudentInputModel);
            if (!result.AnyAffectedRows)
            {
                Snackbar.Add("No se pudo crear el becario.", Severity.Error);
                return;
            }
        }
        catch (InvalidOperationException exception)
        {
            Snackbar.Add(exception.Message, Severity.Error);
            return;
        }
        finally
        {
            _saving = false;
        }

        NavigationManager.NavigateTo(NavRoutes.Students);
    }
}