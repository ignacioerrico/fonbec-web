using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models.Users;
using Fonbec.Web.Logic.Models.Users.Input;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Users;

[PageMetadata(nameof(UsersList), "Lista de usuarios", [FonbecRole.Admin, FonbecRole.Manager])]
public partial class UsersList : AuthenticationRequiredComponentBase
{
    private List<UsersListViewModel> _viewModel = [];

    private string _searchString = string.Empty;

    private bool _isLastNameFirst;

    private IEnumerable<string> _allChapters = [];

    private Dictionary<int, UsersListViewModel> _originalValues = [];

    [Inject]
    public IUserService UserService { get; set; } = null!;

    [Inject]
    public IDialogService DialogService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        Loading = true;

        _viewModel = await UserService.GetAllUsersAsync(FonbecClaim.ChapterId);

        _allChapters = _viewModel.Select(vm => vm.UserChapterName)
            .Distinct()
            .OrderBy(ch => ch);

        Loading = false;
    }

    private bool Filter(UsersListViewModel viewModel) =>
        string.IsNullOrWhiteSpace(_searchString)
        || $"{viewModel.UserFirstName} {viewModel.UserLastName}".ContainsIgnoringAccents(_searchString)
        || $"{viewModel.UserNickName} {viewModel.UserLastName}".ContainsIgnoringAccents(_searchString)
        || viewModel.UserEmail.ContainsIgnoringAccents(_searchString)
        || viewModel.UserPhoneNumber.ContainsIgnoringAccents(_searchString);

    private void StartedEditingItem(UsersListViewModel viewModel)
    {
        _originalValues[viewModel.UserId] = new UsersListViewModel
        {
            UserId = viewModel.UserId,
            UserFirstName = viewModel.UserFirstName,
            UserLastName = viewModel.UserLastName,
            UserNickName = viewModel.UserNickName,
            UserGender = viewModel.UserGender,
            UserEmail = viewModel.UserEmail,
            UserPhoneNumber = viewModel.UserPhoneNumber,
            UserNotes = viewModel.UserNotes
        };
    }

    private async Task CommittedItemChangesAsync(UsersListViewModel viewModel)
    {
        if (_originalValues.TryGetValue(viewModel.UserId, out var originalItem))
        {
            bool hasChanges = originalItem.UserFirstName != viewModel.UserFirstName
                || originalItem.UserLastName != viewModel.UserLastName
                || originalItem.UserNickName != viewModel.UserNickName
                || originalItem.UserGender != viewModel.UserGender
                || originalItem.UserEmail != viewModel.UserEmail
                || originalItem.UserPhoneNumber != viewModel.UserPhoneNumber
                || originalItem.UserNotes != viewModel.UserNotes;

            if (!hasChanges)
            {
                Snackbar.Add("No se realizaron cambios.", Severity.Info);
                _originalValues.Remove(viewModel.UserId);
                return;
            }

            _originalValues.Remove(viewModel.UserId);
        }

        var updateUserInputModel = new UpdateUserInputModel(
            viewModel.UserId,
            viewModel.UserFirstName,
            viewModel.UserLastName,
            viewModel.UserNickName,
            viewModel.UserGender,
            viewModel.UserEmail,
            viewModel.UserPhoneNumber,
            viewModel.UserNotes,
            FonbecClaim.UserId
        );

        var userUpdatedSuccessfully = await UserService.UpdateUserAsync(updateUserInputModel);
        if (!userUpdatedSuccessfully)
        {
            Snackbar.Add("No se pudo actualizar el usuario.", Severity.Error);
        }
    }

    private void CancelledEditingItem(UsersListViewModel viewModel)
    {
        _originalValues.Remove(viewModel.UserId);
    }

    private static Color MudChipColorForRole(string role) =>
        role switch
        {
            FonbecRole.Admin => Color.Secondary,
            FonbecRole.Manager => Color.Warning,
            FonbecRole.Uploader => Color.Info,
            FonbecRole.Reviewer => Color.Primary,
            _ => Color.Default
        };

    private async Task DisableUserAsync(UsersListViewModel? viewModel)
    {
        if (viewModel is null)
        {
            return;
        }

        var message = $"¿Estás seguro de que querés deshabilitar al usuario {viewModel.UserFirstName} {viewModel.UserLastName}?";
        var dialogResult = await DialogService.ShowMessageBox(
            "¡Atención!",
            message,
            yesText: "Sí",
            cancelText: "Cancelar");

        if (dialogResult is null)
        {
            return;
        }

        var disableUserInputModel = new DisableUserInputModel(viewModel.UserId, DisableUser: true, FonbecClaim.UserId);

        var errors = await UserService.DisableUserAsync(disableUserInputModel);

        if (errors.Count > 0)
        {
            foreach (var error in errors)
            {
                Snackbar.Add(error, Severity.Error);
            }

            return;
        }

        viewModel.IsUserActive = false;
    }

    private async Task ReenableUserAsync(UsersListViewModel? viewModel)
    {
        if (viewModel is null)
        {
            return;
        }

        var disableUserInputModel = new DisableUserInputModel(viewModel.UserId, DisableUser: false, FonbecClaim.UserId);
        
        var errors = await UserService.DisableUserAsync(disableUserInputModel);

        if (errors.Count > 0)
        {
            foreach (var error in errors)
            {
                Snackbar.Add(error, Severity.Error);
            }

            return;
        }

        viewModel.IsUserActive = true;
    }

    private async Task DeleteForeverAsync(UsersListViewModel? viewModel)
    {
        if (viewModel is null)
        {
            return;
        }

        var message = string.Format("¿Estás seguro de que querés eliminar al usuario {0} (ID {1})? Este cambio es irreversible.",
            $"{viewModel.UserFirstName} {viewModel.UserLastName}",
            viewModel.UserId);
        var dialogResult = await DialogService.ShowMessageBox(
            "¡Atención!",
            message,
            yesText: "Sí",
            cancelText: "Cancelar");

        if (dialogResult is null)
        {
            return;
        }

        var identityResult = await UserService.DeleteForeverAsync(viewModel.UserId);
        if (!identityResult.Succeeded)
        {
            Snackbar.Add("No se pudo eliminar al usuario.", Severity.Error);

            foreach (var error in identityResult.Errors.Where(e => !string.IsNullOrWhiteSpace(e.Description)))
            {
                Snackbar.Add(error.Description, Severity.Error);
            }

            return;
        }

        _viewModel.Remove(viewModel);
    }
}