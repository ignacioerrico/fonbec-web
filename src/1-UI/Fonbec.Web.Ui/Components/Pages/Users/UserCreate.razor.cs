using Fonbec.Web.Logic.Models.Users.Input;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Constants;
using Fonbec.Web.Ui.Models.User;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Users;

[PageMetadata(nameof(UserCreate), "Alta y actualización de usuarios")]
public partial class UserCreate : AuthenticationRequiredComponentBase
{
    private readonly UserCreateBindModel _bindModel = new();

    private bool _formValidationSucceeded;

    private bool _isFormDisabled;

    private MudTextField<string> _mudTextFieldFirstName = null!;

    private bool _saving;

    private bool SaveButtonDisabled => Loading
                                       || _saving
                                       || _isFormDisabled
                                       || !_formValidationSucceeded;

    [Inject]
    public IUserService UserService { get; set; } = null!;

    [Inject]
    public IDialogService DialogService { get; set; } = null!;

    private async Task OnChaptersLoaded(int chaptersCount)
    {
        _isFormDisabled = chaptersCount == 0;

        if (!_isFormDisabled)
        {
            await _mudTextFieldFirstName.FocusAsync();
        }
    }

    private async Task SaveAsync()
    {
        _saving = true;

        var (isEmailUnique, userFullNameOrNull) = await UserService.ValidateUniqueEmailAsync(_bindModel.UserEmail);
        if (!isEmailUnique)
        {
            var message = $"La dirección de correo {_bindModel.UserEmail} ya la tiene el usuario {userFullNameOrNull}.";

            Snackbar.Add(message, Severity.Error);
            _saving = false;
            return;
        }

        var validateUniqueFullNameOutputModel = await UserService.ValidateUniqueFullNameAsync(_bindModel.UserFirstName, _bindModel.UserLastName);
        if (!validateUniqueFullNameOutputModel.IsFullNameUnique)
        {
            var message = $"Ya existe un usuario que se llama {_bindModel.UserFirstName} {_bindModel.UserLastName}. ¿Querés crear este de todas formas?";
            
            var dialogResult = await DialogService.ShowMessageBox(
                "¡Atención!",
                message,
                yesText: "Sí",
                cancelText: "Cancelar");

            if (dialogResult is null)
            {
                _saving = false;
                return;
            }
        }

        var createUserInputModel = new CreateUserInputModel
        (
            _bindModel.UserChapterId,
            _bindModel.UserFirstName,
            _bindModel.UserLastName,
            _bindModel.UserNickName,
            _bindModel.UserGender,
            _bindModel.UserEmail,
            _bindModel.UserPhoneNumber,
            _bindModel.UserRole,
            CurrentUserId
        );

        var (createdUserId, errors) = await UserService.CreateUserAsync(createUserInputModel);
        if (errors.Count > 0)
        {
            foreach (var error in errors)
            {
                Snackbar.Add(error, Severity.Error);
            }
        }
        else if (createdUserId <= 0)
        {
            Snackbar.Add("No se pudo crear el usuario.", Severity.Error);
        }
        else
        {
            Snackbar.Add($"El usuario {_bindModel.UserFirstName} {_bindModel.UserLastName} (ID: {createdUserId}) se creó exitosamente.", Severity.Success);
        }

        _saving = false;

        NavigationManager.NavigateTo(NavRoutes.Users);
    }
}