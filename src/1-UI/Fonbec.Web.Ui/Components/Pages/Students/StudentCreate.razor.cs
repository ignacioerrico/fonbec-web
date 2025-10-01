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

    private bool _isFormDisabled;

    private bool _formValidationSucceeded;

    private MudTextField<string> _mudTextFieldName = null!;

    private bool _saving;

    private bool SaveButtonDisabled => Loading
                                       || _saving
                                       || _isFormDisabled
                                       || !_formValidationSucceeded;

    [Inject]
    public IStudentService StudentService { get; set; } = null!;

    private async Task OnFacilitatorsLoaded(int totalFacilitators)
    {
        _isFormDisabled = totalFacilitators == 0;

        if (!_isFormDisabled)
        {
            await _mudTextFieldName.FocusAsync();
        }
    }

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