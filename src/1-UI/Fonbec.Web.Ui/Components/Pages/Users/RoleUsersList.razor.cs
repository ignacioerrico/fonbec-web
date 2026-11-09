using System.Globalization;
using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.DataModels.Users;
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

    private HashSet<int> _userIdsWithLinkedStudents = [];

    private Dictionary<int, List<FacilitatorStudentDataModel>> _studentsByFacilitator = [];

    private static readonly CompareInfo SpanishCompare = CultureInfo.GetCultureInfo("es-AR").CompareInfo;

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

    [Parameter]
    public bool ShowStudents { get; set; }

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

        if (ShowStudents)
        {
            var facilitatorIds = _viewModel.Select(user => user.UserId).ToList();
            var linked = await UserService.GetUserIdsWithLinkedStudentsAsync(facilitatorIds);
            _userIdsWithLinkedStudents = linked.ToHashSet();

            var students = await UserService.GetFacilitatorStudentsAsync(facilitatorIds);
            _studentsByFacilitator = students
                .GroupBy(student => student.FacilitatorId)
                .ToDictionary(group => group.Key, group => group.ToList());

            foreach (var user in _viewModel)
            {
                user.LinkedStudentCount = _studentsByFacilitator.TryGetValue(user.UserId, out var linkedStudents)
                    ? linkedStudents.Count
                    : 0;
            }
        }

        Loading = false;
    }

    private bool HasLinkedStudents(UsersListViewModel viewModel) =>
        ShowStudents && _userIdsWithLinkedStudents.Contains(viewModel.UserId);

    private List<string> StudentChipLabels(UsersListViewModel viewModel)
    {
        if (!_studentsByFacilitator.TryGetValue(viewModel.UserId, out var students))
        {
            return [];
        }

        return students
            .Select(student => _isLastNameFirst
                ? $"{student.LastName}, {student.FirstName}"
                : $"{student.FirstName} {student.LastName}")
            .OrderBy(name => name, Comparer<string>.Create(CompareStudentNames))
            .ToList();
    }

    private static int CompareStudentNames(string left, string right) =>
        SpanishCompare.Compare(left, right, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace);

    private bool CanDisable(UsersListViewModel viewModel) =>
        !HasLinkedStudents(viewModel)
        && viewModel.UserId != FonbecClaim.UserId
        && viewModel.CanUserBeLockedOut;

    private string DisableTooltip(UsersListViewModel viewModel) =>
        HasLinkedStudents(viewModel)
            ? L["CannotDisable"]
            : L["Disable"];

    private string? ValidateEmail(string value) => Fonbec.Web.Logic.Util.ContactFieldValidator.ValidateEmail(value) is null ? null : L["InvalidEmail"].Value;
    private string? ValidatePhone(string value) => Fonbec.Web.Logic.Util.ContactFieldValidator.ValidatePhone(value) is null ? null : L["InvalidPhone"].Value;

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
            Snackbar.Add(L["UpdateUserFailed"], Severity.Error);
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

        var message = string.Format(L["ResetConfirm"],
            $"{viewModel.UserFirstName} {viewModel.UserLastName}",
            viewModel.UserId);
        var dialogResult = await DialogService.ShowMessageBox(
            L["Warning"],
            message,
            yesText: L["Yes"],
            cancelText: L["No"]);

        if (dialogResult is null)
        {
            Loading = false;
            return;
        }

        var identityResult = await UserService.ResetPasswordAsync(viewModel.UserId, viewModel.UserEmail);

        Loading = false;

        if (identityResult.Succeeded)
        {
            Snackbar.Add(L["PasswordSent", viewModel.UserEmail], Severity.Success);
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
                ? L["ImagePermissionGranted"]
                : L["ImagePermissionRemoved"],
            Severity.Success);
    }

    private async Task DisableUserAsync(UsersListViewModel? viewModel)
    {
        if (viewModel is null || !CanDisable(viewModel))
        {
            return;
        }

        Loading = true;

        var message = L["DisableConfirm", viewModel.UserFirstName, viewModel.UserLastName];
        var dialogResult = await DialogService.ShowMessageBox(
            L["Warning"],
            message,
            yesText: L["Yes"],
            cancelText: L["Cancel"]);

        if (dialogResult is null)
        {
            Loading = false;
            return;
        }

        var disableUserInputModel = new DisableUserInputModel(viewModel.UserId, DisableUser: true, FonbecClaim.UserId);
        var errors = await UserService.DisableUserAsync(disableUserInputModel, UserRole, FonbecClaim.ChapterId);

        Loading = false;

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

        Loading = true;

        var errors = await UserService.DisableUserAsync(disableUserInputModel, UserRole, FonbecClaim.ChapterId);

        Loading = false;

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
}