using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models.Users;
using Fonbec.Web.Logic.Models.Users.Input;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Users;

public partial class RoleUsersList : AuthenticationRequiredComponentBase
{
    private List<UsersListViewModel> _viewModel = [];

    private UsersListViewModel _originalViewModel = new();

    private string _searchString = string.Empty;

    private bool _isLastNameFirst;

    [Parameter]
    public string Title { get; set; } = string.Empty;

    [Parameter]
    public string Role { get; set; } = string.Empty;

    [Parameter]
    public string EmptyMessage { get; set; } = string.Empty;

    [Parameter]
    public string CreateRoute { get; set; } = string.Empty;

    [Parameter]
    public string CreateTooltip { get; set; } = string.Empty;

    [Parameter]
    public string CreatePolicy { get; set; } = string.Empty;

    [Parameter]
    public bool ShowDigitalImprovement { get; set; }

    [Inject]
    public IUserService UserService { get; set; } = null!;

    [Inject]
    public IDialogService DialogService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        Loading = true;

        var users = await UserService.GetAllUsersAsync(FonbecClaim.ChapterId);
        _viewModel = users.Where(user => user.UserRole == Role).ToList();

        Loading = false;
    }

    private bool Filter(UsersListViewModel viewModel) =>
        string.IsNullOrWhiteSpace(_searchString)
        || $"{viewModel.UserFirstName} {viewModel.UserLastName}".ContainsIgnoringAccents(_searchString)
        || (!string.IsNullOrEmpty(viewModel.UserNickName)
            && $"{viewModel.UserNickName} {viewModel.UserLastName}".ContainsIgnoringAccents(_searchString))
        || viewModel.UserEmail.Contains(_searchString, StringComparison.OrdinalIgnoreCase)
        || (!string.IsNullOrEmpty(viewModel.UserPhoneNumber)
            && viewModel.UserPhoneNumber.ContainsIgnoringSpaces(_searchString));

    private void StartedEditingItem(UsersListViewModel originalViewModel) =>
        _originalViewModel = originalViewModel.DeepClone();

    private async Task CommittedItemChangesAsync(UsersListViewModel modifiedViewModel)
    {
        if (_originalViewModel.IsEqualTo(modifiedViewModel))
        {
            Snackbar.Add("No se realizaron cambios.", Severity.Info);
            return;
        }

        var updateUserInputModel = new UpdateUserInputModel(
            modifiedViewModel.UserId,
            modifiedViewModel.UserFirstName,
            modifiedViewModel.UserLastName,
            modifiedViewModel.UserNickName,
            modifiedViewModel.UserGender,
            modifiedViewModel.UserEmail,
            modifiedViewModel.UserPhoneNumber,
            modifiedViewModel.UserNotes,
            FonbecClaim.UserId
        );

        Loading = true;

        var userUpdatedSuccessfully = await UserService.UpdateUserAsync(updateUserInputModel);

        Loading = false;

        if (!userUpdatedSuccessfully)
        {
            Snackbar.Add("No se pudo actualizar el usuario.", Severity.Error);
            RevertItemChanges(modifiedViewModel.UserId);
        }
    }

    private void RevertItemChanges(int userId)
    {
        var index = _viewModel.FindIndex(vm => vm.UserId == userId);
        if (index >= 0)
        {
            _viewModel[index] = _originalViewModel.DeepClone();
        }
    }

    private async Task ResetPasswordAsync(UsersListViewModel? viewModel)
    {
        if (viewModel is null)
        {
            return;
        }

        Loading = true;

        var message = string.Format("Estás por restablecer la contraseña del usuario {0} (ID {1}).  El usuario va a recibir su nueva contraseña por correo electrónico.  ¿Querés continuar?",
            $"{viewModel.UserFirstName} {viewModel.UserLastName}",
            viewModel.UserId);
        var dialogResult = await DialogService.ShowMessageBox(
            "¡Atención!",
            message,
            yesText: "Sí",
            cancelText: "No");

        if (dialogResult is null)
        {
            Loading = false;
            return;
        }

        var identityResult = await UserService.ResetPasswordAsync(viewModel.UserId, viewModel.UserEmail);

        Loading = false;

        if (identityResult.Succeeded)
        {
            Snackbar.Add($"La nueva contraseña fue enviada a {viewModel.UserEmail}", Severity.Success);
        }
        else
        {
            foreach (var error in identityResult.Errors.Where(e => !string.IsNullOrWhiteSpace(e.Description)))
            {
                Snackbar.Add(error.Description, Severity.Error);
            }
        }
    }

    private async Task SetDigitalImprovementGrantAsync(UsersListViewModel viewModel, bool granted)
    {
        viewModel.HasDigitalImprovementGrant = granted;

        Loading = true;

        var result = await UserService.SetDigitalImprovementGrantAsync(
            viewModel.UserId,
            granted,
            UserRole,
            FonbecClaim.ChapterId);

        Loading = false;

        if (!result.IsSuccess)
        {
            viewModel.HasDigitalImprovementGrant = !granted;

            foreach (var error in result.Errors ?? [])
            {
                Snackbar.Add(error, Severity.Error);
            }

            return;
        }

        Snackbar.Add(
            granted
                ? "Se le dio el permiso para mejorar imágenes."
                : "Se le sacó el permiso para mejorar imágenes.",
            Severity.Success);
    }
}