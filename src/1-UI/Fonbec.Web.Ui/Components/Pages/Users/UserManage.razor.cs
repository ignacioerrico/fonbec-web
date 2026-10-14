using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.Authorization;
using Fonbec.Web.Logic.Models.Users.Input;
using Fonbec.Web.Logic.Models.Users.Output;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Constants;
using Fonbec.Web.Ui.Models.User;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Users;

[PageMetadata(nameof(UserManage), "Administrar usuario", [FonbecRole.Admin])]
public partial class UserManage : AuthenticationRequiredComponentBase
{
    private GetUserOutputModel? _user;

    private bool _missing;

    private MudForm? _form;

    private bool _formIsValid;

    private bool _saving;

    private bool _canImproveImages;

    private bool _hasLinkedStudents;

    private string _firstName = string.Empty;

    private string _lastName = string.Empty;

    private string _nickName = string.Empty;

    private Gender _gender = Gender.Male;

    private string _email = string.Empty;

    private string _phoneNumber = string.Empty;

    private string _notes = string.Empty;

    private readonly List<UserPermissionsBindModel> _permissions = [];

    private readonly List<bool> _originalPermissions = [];

    [Parameter]
    public int UserId { get; set; }

    [Inject]
    public IUserService UserService { get; set; } = null!;

    [Inject]
    public IDialogService DialogService { get; set; } = null!;

    [Inject]
    public List<PageAccessInfo> AllPages { get; set; } = null!;

    private bool IsSelf => UserId == FonbecClaim.UserId;

    private bool SupportsImageImprovement =>
        _user?.UserRole is FonbecRole.Manager or FonbecRole.Reviewer;

    private bool CanDisable => !IsSelf && _user is { CanBeLockedOut: true } && !_hasLinkedStudents;

    private string DisableTooltip =>
        _hasLinkedStudents
            ? UserMessages.CannotDisableFacilitatorWithStudents
            : IsSelf
                ? "No podés deshabilitar tu propia cuenta."
                : "Deshabilitar";

    private string BackRoute =>
        _user?.UserRole == FonbecRole.Admin ? NavRoutes.Admins : NavRoutes.Chapters;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        Loading = true;

        var user = await UserService.GetUserAsync(UserId);
        if (user is null || string.IsNullOrEmpty(user.UserRole))
        {
            _user = null;
            _missing = true;
            Loading = false;
            return;
        }

        _user = user;
        _missing = false;
        CopyProfile(user);
        _canImproveImages = SupportsImageImprovement && await UserService.HasDigitalImprovementGrantAsync(UserId);
        _hasLinkedStudents = user.UserRole == FonbecRole.Uploader
                             && (await UserService.GetUserIdsWithLinkedStudentsAsync([UserId])).Contains(UserId);
        await LoadPermissionsAsync(user);

        Loading = false;
    }

    private void CopyProfile(GetUserOutputModel user)
    {
        _firstName = user.FirstName;
        _lastName = user.LastName;
        _nickName = user.UserNickName ?? string.Empty;
        _gender = user.Gender == Gender.Unknown ? Gender.Male : user.Gender;
        _email = user.Email;
        _phoneNumber = user.PhoneNumber ?? string.Empty;
        _notes = user.Notes ?? string.Empty;
    }

    private async Task LoadPermissionsAsync(GetUserOutputModel user)
    {
        _permissions.Clear();
        _originalPermissions.Clear();

        var claim = await UserService.GetFonbecAuthClaim(UserId);
        foreach (var page in AllPages.Where(page => page.Roles.Contains(user.UserRole)))
        {
            var item = new UserPermissionsBindModel
            {
                IsChecked = UserService.HasPermission(claim, user.UserRole, page.Codename),
                Page = page.Codename,
                Description = page.Description,
            };
            _permissions.Add(item);
            _originalPermissions.Add(item.IsChecked);
        }
    }

    private async Task SaveProfileAsync()
    {
        if (_user is null || _form is null)
        {
            return;
        }

        await _form.Validate();
        if (!_formIsValid)
        {
            return;
        }

        _saving = true;

        var emailChanged = !string.Equals(_email.Trim(), _user.Email.Trim(), StringComparison.OrdinalIgnoreCase);
        if (emailChanged)
        {
            var (isEmailUnique, ownerName) = await UserService.ValidateUniqueEmailAsync(_email);
            if (!isEmailUnique)
            {
                Snackbar.Add(
                    $"La dirección de correo {_email.Trim()} ya la tiene el usuario {ownerName}.",
                    Severity.Error);
                _saving = false;
                return;
            }
        }

        var updated = await UserService.UpdateUserAsync(new UpdateUserInputModel(
            UserId,
            _firstName,
            _lastName,
            _nickName,
            _gender,
            _email,
            _phoneNumber,
            _notes,
            FonbecClaim.UserId));

        _saving = false;

        if (!updated)
        {
            Snackbar.Add("No se pudo actualizar el usuario.", Severity.Error);
            return;
        }

        Snackbar.Add("Se guardaron los datos.", Severity.Success);
        await RefreshUserAsync();
    }

    private async Task RefreshUserAsync()
    {
        var user = await UserService.GetUserAsync(UserId);
        if (user is null || string.IsNullOrEmpty(user.UserRole))
        {
            return;
        }

        _user = user;
        CopyProfile(user);
    }

    private async Task DisableAsync()
    {
        if (_user is null || !CanDisable)
        {
            return;
        }

        var confirmed = await DialogService.ShowMessageBox(
            "¡Atención!",
            $"¿Estás seguro de que querés deshabilitar al usuario {_user.FirstName} {_user.LastName}?",
            yesText: "Sí",
            cancelText: "Cancelar");

        if (confirmed is null)
        {
            return;
        }

        await SetActiveAsync(disable: true);
    }

    private Task ReenableAsync() => SetActiveAsync(disable: false);

    private async Task SetActiveAsync(bool disable)
    {
        Loading = true;

        var errors = await UserService.DisableUserAsync(
            new DisableUserInputModel(UserId, disable, FonbecClaim.UserId),
            UserRole,
            FonbecClaim.ChapterId);

        Loading = false;

        if (errors.Count > 0)
        {
            foreach (var error in errors)
            {
                Snackbar.Add(error, Severity.Error);
            }

            return;
        }

        await RefreshUserAsync();
    }

    private async Task ResetPasswordAsync()
    {
        if (_user is null || IsSelf || string.IsNullOrWhiteSpace(_user.Email))
        {
            return;
        }

        var confirmed = await DialogService.ShowMessageBox(
            "¡Atención!",
            $"Estás por restablecer la contraseña del usuario {_user.FirstName} {_user.LastName} (ID {UserId}). El usuario va a recibir su nueva contraseña por correo electrónico. ¿Querés continuar?",
            yesText: "Sí",
            cancelText: "No");

        if (confirmed is null)
        {
            return;
        }

        Loading = true;
        var result = await UserService.ResetPasswordAsync(UserId, _user.Email);
        Loading = false;

        if (result.Succeeded)
        {
            Snackbar.Add($"La nueva contraseña fue enviada a {_user.Email}", Severity.Success);
            return;
        }

        foreach (var error in result.Errors.Where(error => !string.IsNullOrWhiteSpace(error.Description)))
        {
            Snackbar.Add(error.Description, Severity.Error);
        }
    }

    private async Task SetImageImprovementAsync(bool granted)
    {
        if (_user is null || !SupportsImageImprovement)
        {
            return;
        }

        _canImproveImages = granted;
        Loading = true;

        var result = await UserService.SetDigitalImprovementGrantAsync(
            UserId,
            granted,
            UserRole,
            FonbecClaim.ChapterId);

        Loading = false;

        if (!result.IsSuccess)
        {
            _canImproveImages = !granted;
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

    private void SetAllPermissions(bool granted) => _permissions.ForEach(item => item.IsChecked = granted);

    private void ResetPermissions()
    {
        for (var index = 0; index < _permissions.Count; index++)
        {
            _permissions[index].IsChecked = _originalPermissions[index];
        }
    }

    private async Task SavePermissionsAsync()
    {
        var deniedPages = _permissions.Where(item => !item.IsChecked).Select(item => item.Page);
        await UserService.SetFonbecAuthClaim(UserId, deniedPages);

        for (var index = 0; index < _permissions.Count; index++)
        {
            _originalPermissions[index] = _permissions[index].IsChecked;
        }

        Snackbar.Add("Se guardaron los permisos.", Severity.Success);
    }
}