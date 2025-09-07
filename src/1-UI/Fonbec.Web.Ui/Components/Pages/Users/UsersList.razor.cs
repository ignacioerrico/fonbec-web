using Fonbec.Web.DataAccess.Constants;
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

    private bool _sortByLastName;

    private string _rolesFilterIcon = Icons.Material.Outlined.FilterAlt;
    private bool _isRolesFilterOpen;
    private bool _areAllRolesSelected = true;
    private HashSet<string> _selectedRoles = [];
    private HashSet<string> _selectedRolesBeforeFilter = [];
    private FilterDefinition<AllUsersViewModel> _filterDefinition = null!;

    [Inject]
    public IUserService UserService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        Loading = true;

        _userId = await GetAuthenticatedUserIdAsync();

        _allUsers = await UserService.GetAllUsersAsync();

        _selectedRoles = FonbecRole.AllRoles.ToHashSet();
        _selectedRolesBeforeFilter = _selectedRoles.ToHashSet();

        _filterDefinition = new FilterDefinition<AllUsersViewModel>
        {
            FilterFunction = vm => vm.Roles.Any(r => _selectedRolesBeforeFilter.Contains(r))
        };

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

        var userUpdatedSuccessfully = await UserService.UpdateUserAsync(updateUserInputModel);
        if (!userUpdatedSuccessfully)
        {
            Snackbar.Add("No se pudo actualizar el usuario.", Severity.Error);
        }
    }

    private async Task OpenDisableDialogAsync(AllUsersViewModel viewModel)
    {

    }

    private void OnSelectAllRolesChanged(bool areAllRolesSelected)
    {
        _areAllRolesSelected = areAllRolesSelected;

        if (areAllRolesSelected)
        {
            _selectedRoles = FonbecRole.AllRoles.ToHashSet();
        }
        else
        {
            _selectedRoles.Clear();
        }
    }

    private void SelectedChanged(string role, bool isSelected)
    {
        if (isSelected)
        {
            _selectedRoles.Add(role);
        }
        else
        {
            _selectedRoles.Remove(role);
        }

        _areAllRolesSelected = _selectedRoles.Count == FonbecRole.AllRoles.Length;
    }

    private async Task ClearFilterAsync(FilterContext<AllUsersViewModel> context)
    {
        _areAllRolesSelected = true;
        _selectedRoles = FonbecRole.AllRoles.ToHashSet();
        _selectedRolesBeforeFilter = _selectedRoles.ToHashSet();
        _rolesFilterIcon = Icons.Material.Outlined.FilterAlt;
        await context.Actions.ClearFilterAsync(_filterDefinition);
        _isRolesFilterOpen = false;
    }

    private async Task ApplyFilterAsync(FilterContext<AllUsersViewModel> context)
    {
        _selectedRolesBeforeFilter = _selectedRoles.ToHashSet();
        _rolesFilterIcon = _selectedRolesBeforeFilter.Count == FonbecRole.AllRoles.Length
            ? Icons.Material.Outlined.FilterAlt
            : Icons.Material.Filled.FilterAlt;
        await context.Actions.ApplyFilterAsync(_filterDefinition);
        _isRolesFilterOpen = false;
    }
}