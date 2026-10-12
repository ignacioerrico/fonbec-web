using Fonbec.Web.Logic.Models.Users.Input;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Models.User;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Users;

public partial class RoleUserCreate : AuthenticationRequiredComponentBase
{
    private readonly UserCreateBindModel _bindModel = new()
    {
        UserFirstName = string.Empty,
        UserLastName = string.Empty,
        UserNickName = string.Empty,
        UserEmail = string.Empty,
        UserPhoneNumber = string.Empty,
        UserNotes = string.Empty,
    };

    private bool _formValidationSucceeded;

    private bool _hasChapter;

    private bool _saving;

    private bool SaveButtonDisabled => Loading
                                       || _saving
                                       || !_hasChapter
                                       || !_formValidationSucceeded;

    [Parameter]
    public string Title { get; set; } = string.Empty;

    [Parameter]
    public string Role { get; set; } = string.Empty;

    [Parameter]
    public string CancelRoute { get; set; } = string.Empty;

    [Inject]
    public IUserService UserService { get; set; } = null!;

    [Inject]
    public IDialogService DialogService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        _hasChapter = FonbecClaim.ChapterId is not null;
    }

    private async Task SaveAsync()
    {
        if (FonbecClaim.ChapterId is null)
        {
            Snackbar.Add("La filial no es válida.", Severity.Error);
            return;
        }

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
            FonbecClaim.ChapterId.Value,
            _bindModel.UserFirstName,
            _bindModel.UserLastName,
            _bindModel.UserNickName,
            _bindModel.UserGender,
            _bindModel.UserEmail,
            _bindModel.UserPhoneNumber,
            _bindModel.UserNotes,
            Role,
            FonbecClaim.UserId
        );

        var (createdUserId, errors) = await UserService.CreateUserAsync(createUserInputModel);
        if (errors.Count > 0)
        {
            foreach (var error in errors)
            {
                Snackbar.Add(error, Severity.Error);
            }

            _saving = false;
            return;
        }

        if (createdUserId <= 0)
        {
            Snackbar.Add("No se pudo crear el usuario.", Severity.Error);
            _saving = false;
            return;
        }

        Snackbar.Add($"El usuario {_bindModel.UserFirstName} {_bindModel.UserLastName} (ID: {createdUserId}) se creó exitosamente.", Severity.Success);
        _saving = false;

        NavigationManager.NavigateTo(CancelRoute);
    }
}