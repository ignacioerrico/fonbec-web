using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models.Users;
using Fonbec.Web.Logic.Models.Users.Input;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Users;

public partial class UsersList : AuthenticationRequiredComponentBase
{
    private List<AllUsersViewModel> _allUsers = [];

    private int _userId;

    private string _searchString = string.Empty;

    [Inject]
    public IUserService UserService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        Loading = true;

        _userId = await GetAuthenticatedUserIdAsync();

        _allUsers = await UserService.GetAllUsersAsync();

        Loading = false;
    }

    private bool Filter(AllUsersViewModel viewModel) =>
        string.IsNullOrWhiteSpace(_searchString)
        || $"{viewModel.UserFirstName} {viewModel.UserLastName}".ContainsIgnoringAccents(_searchString)
        || $"{viewModel.UserNickName} {viewModel.UserLastName}".ContainsIgnoringAccents(_searchString)
        || viewModel.UserEmail.ContainsIgnoringAccents(_searchString)
        || viewModel.UserPhoneNumber.ContainsIgnoringAccents(_searchString);

    private async Task CommittedItemChangesAsync(AllUsersViewModel viewModel)
    {
        var updateUserInputModel = new UpdateUserInputModel(
            viewModel.UserId,
            viewModel.UserFirstName,
            viewModel.UserLastName,
            viewModel.UserNickName,
            viewModel.UserGender,
            viewModel.UserEmail,
            viewModel.UserPhoneNumber,
            _userId
        );

        var identityResult = await UserService.UpdateUserAsync(updateUserInputModel);
        if (!identityResult.Succeeded)
        {
            Snackbar.Add("No se pudo actualizar el usuario.", Severity.Error);
        }
    }

    private async Task OpenDisableDialogAsync(AllUsersViewModel viewModel)
    {

    }
}