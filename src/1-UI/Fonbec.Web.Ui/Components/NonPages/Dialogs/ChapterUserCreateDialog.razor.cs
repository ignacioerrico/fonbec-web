using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.Models.Users.Input;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Models.User;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.NonPages.Dialogs;

public partial class ChapterUserCreateDialog
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

    private bool _saving;

    private bool SaveButtonDisabled => _saving || !_formValidationSucceeded;

    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    public int? ChapterId { get; set; }

    [Parameter]
    public string Role { get; set; } = string.Empty;

    [Parameter]
    public int CreatedById { get; set; }

    [Inject]
    public IUserService UserService { get; set; } = null!;

    [Inject]
    public IDialogService DialogService { get; set; } = null!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = null!;

    private void Cancel() => MudDialog.Cancel();

    private async Task SaveAsync()
    {
        var isAdmin = Role == FonbecRole.Admin;
        if (!isAdmin && ChapterId is not > 0)
        {
            Snackbar.Add("La filial no es válida.", Severity.Error);
            return;
        }

        _saving = true;

        var (isEmailUnique, userFullNameOrNull) = await UserService.ValidateUniqueEmailAsync(_bindModel.UserEmail);
        if (!isEmailUnique)
        {
            Snackbar.Add(
                $"La dirección de correo {_bindModel.UserEmail} ya la tiene el usuario {userFullNameOrNull}.",
                Severity.Error);
            _saving = false;
            return;
        }

        var validateUniqueFullNameOutputModel = await UserService.ValidateUniqueFullNameAsync(
            _bindModel.UserFirstName,
            _bindModel.UserLastName);
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

        var createUserInputModel = new CreateUserInputModel(
            isAdmin ? null : ChapterId,
            _bindModel.UserFirstName,
            _bindModel.UserLastName,
            _bindModel.UserNickName,
            _bindModel.UserGender,
            _bindModel.UserEmail,
            _bindModel.UserPhoneNumber,
            _bindModel.UserNotes,
            Role,
            CreatedById);

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

        Snackbar.Add(
            $"El usuario {_bindModel.UserFirstName} {_bindModel.UserLastName} (ID: {createdUserId}) se creó exitosamente.",
            Severity.Success);
        _saving = false;
        MudDialog.Close(DialogResult.Ok(true));
    }
}