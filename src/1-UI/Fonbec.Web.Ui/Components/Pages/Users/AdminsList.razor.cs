using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.Models.Users;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Components.NonPages.Dialogs;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Users;

[PageMetadata(nameof(AdminsList), "Lista de administradores", [FonbecRole.Admin])]
public partial class AdminsList : AuthenticationRequiredComponentBase
{
    private List<UsersListViewModel> _admins = [];

    [Inject]
    public IUserService UserService { get; set; } = null!;

    [Inject]
    public IDialogService DialogService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        Loading = true;
        _admins = await UserService.GetAdminsAsync();
        Loading = false;
    }

    private async Task AddAdminAsync()
    {
        var parameters = new DialogParameters<ChapterUserCreateDialog>
        {
            { dialog => dialog.ChapterId, null },
            { dialog => dialog.Role, FonbecRole.Admin },
            { dialog => dialog.CreatedById, FonbecClaim.UserId },
        };

        var dialog = await DialogService.ShowAsync<ChapterUserCreateDialog>("Agregar administrador", parameters);
        var result = await dialog.Result;
        if (result is null || result.Canceled)
        {
            return;
        }

        await LoadAsync();
    }
}