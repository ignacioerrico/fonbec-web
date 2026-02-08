using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.Models.Students.Input;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Constants;
using Fonbec.Web.Ui.Models.Student;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Students;

[PageMetadata(nameof(StudentCreate), "Crear y actualizar becario", [FonbecRole.Manager])]
public partial class StudentCreate : AuthenticationRequiredComponentBase
{
    private readonly StudentCreateBindModel _bindModel = new();

    private bool IsFormDisabled => !_anyChapters || !_anyFacilitators;
    private bool _anyChapters;
    private bool _anyFacilitators;

    private bool _formValidationSucceeded;

    private bool _saving;

    private bool SaveButtonDisabled => Loading
                                       || _saving
                                       || IsFormDisabled
                                       || !_formValidationSucceeded;

    [Inject]
    public IStudentService StudentService { get; set; } = null!;

    private async Task OnChaptersLoaded(int chaptersCount) =>
        _anyChapters = chaptersCount > 0;

    private async Task OnFacilitatorsLoaded(int totalFacilitators) =>
        _anyFacilitators = totalFacilitators > 0;

    private async Task Save()
    {
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

        var result = await StudentService.CreateStudentAsync(createStudentInputModel);
        if (!result.AnyAffectedRows)
        {
            Snackbar.Add("No se pudo crear el becario.", Severity.Error);
        }

        _saving = false;

        NavigationManager.NavigateTo(NavRoutes.Students);
    }
}