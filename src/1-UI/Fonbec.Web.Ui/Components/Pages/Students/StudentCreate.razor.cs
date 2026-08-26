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

    private bool IsFormDisabled => _facilitatorsLoaded && !_anyFacilitators && _bindModel.ChapterId > 0;
    private bool _anyFacilitators;
    private bool _facilitatorsLoaded;

    private bool _formValidationSucceeded;
    private bool _saving;

    private bool SaveButtonDisabled => Loading
                                       || _saving
                                       || IsFormDisabled
                                       || !_formValidationSucceeded;

    [Inject]
    public IStudentService StudentService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        if (FonbecClaim?.ChapterId.HasValue == true)
        {
            _bindModel.ChapterId = FonbecClaim.ChapterId.Value;
        }
    }
    private async Task NumberOfFacilitatorsLoaded(int totalFacilitators)
    {
        _anyFacilitators = totalFacilitators > 0;
        _facilitatorsLoaded = true;
    }

    private async Task Save()
    {
        if (FonbecClaim?.ChapterId.HasValue == true)
        {
            _bindModel.ChapterId = FonbecClaim.ChapterId.Value;
        }
        if (_bindModel.ChapterId == 0)
        {
            Snackbar.Add("La filial no es válida.", Severity.Error);
            return;
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

        var result = await StudentService.CreateStudentAsync(createStudentInputModel);
        if (!result.AnyAffectedRows)
        {
            Snackbar.Add("No se pudo crear el becario.", Severity.Error);
        }

        _saving = false;

        NavigationManager.NavigateTo(NavRoutes.Students);
    }
}