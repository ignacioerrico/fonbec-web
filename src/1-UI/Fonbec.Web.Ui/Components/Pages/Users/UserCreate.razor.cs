using Fonbec.Web.Logic.Models.Users;
using Fonbec.Web.Logic.Models.Users.Input;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Constants;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Users;

public partial class UserCreate : AuthenticationRequiredComponentBase
{
    private readonly UserCreateViewModel _viewModel = new();

    private int _userId;

    private bool _formValidationSucceeded;

    private MudTextField<string> _mudTextFieldFirstName = null!;

    private bool _saving;

    private bool SaveButtonDisabled => Loading
                                       || _saving
                                       || !_formValidationSucceeded;

    [Inject]
    public IUserService UserService { get; set; } = null!;

    [Inject]
    public IDialogService DialogService { get; set; } = null!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await _mudTextFieldFirstName.FocusAsync();
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    protected override async Task OnInitializedAsync()
    {
        _userId = await GetAuthenticatedUserIdAsync();
    }

    private async Task SaveAsync()
    {
        _saving = true;

        var (isEmailUnique, userFullNameOrNull) = await UserService.ValidateUniqueEmailAsync(_viewModel.UserEmail);
        if (!isEmailUnique)
        {
            var message = $"La dirección de correo {_viewModel.UserEmail} ya la tiene el usuario {userFullNameOrNull}.";

            Snackbar.Add(message, Severity.Error);
            _saving = false;
            return;
        }

        var validateUniqueFullNameOutputModel = await UserService.ValidateUniqueFullNameAsync(_viewModel.UserFirstName, _viewModel.UserLastName);
        if (!validateUniqueFullNameOutputModel.IsFullNameUnique)
        {
            var message = $"Ya existe un usuario que se llama {_viewModel.UserFirstName} {_viewModel.UserLastName}. ¿Querés crear este de todas formas?";
            
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
            _viewModel.UserFirstName,
            _viewModel.UserLastName,
            _viewModel.UserNickName,
            _viewModel.UserRoles,
            _viewModel.UserGender,
            _viewModel.UserEmail,
            _viewModel.UserPhoneNumber,
            _userId
        );

        var (userId, errors) = await UserService.CreateUserAsync(createUserInputModel);
        if (errors.Count > 0)
        {
            foreach (var error in errors)
            {
                Snackbar.Add(error, Severity.Error);
            }
        }
        else if (userId <= 0)
        {
            Snackbar.Add("No se pudo crear el usuario.", Severity.Error);
        }
        else
        {
            Snackbar.Add($"El usuario {_viewModel.UserFirstName} {_viewModel.UserLastName} (ID: {userId}) se creó exitosamente.", Severity.Success);
        }

        _saving = false;

        NavigationManager.NavigateTo(NavRoutes.Users);
    }
}