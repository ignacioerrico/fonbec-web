using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.Authorization;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Constants;
using Fonbec.Web.Ui.Models.User;
using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.Pages.Users;

[PageMetadata(nameof(UserPermissions), "Permisos de usuario", [FonbecRole.Admin])]
public partial class UserPermissions : AuthenticationRequiredComponentBase
{
    private List<UserPermissionsBindModel> CheckBoxItems { get; } = [];

    private readonly List<bool> _originalSelection = [];

    [Parameter]
    public int UserId { get; set; }

    [Inject]
    public List<PageAccessInfo> AllPages { get; set; } = null!;

    [Inject]
    public IUserService UserService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        var fonbecAuthClaim = await UserService.GetFonbecAuthClaim(UserId);

        foreach (var page in AllPages)
        {
            var checkBoxItem = new UserPermissionsBindModel
            {
                IsChecked = UserService.HasPermission(fonbecAuthClaim, page.Codename),
                Page = page.Codename,
                Description = page.Description,
            };

            CheckBoxItems.Add(checkBoxItem);

            _originalSelection.Add(checkBoxItem.IsChecked);
        }
    }

    private void SetAllTo(bool b) => CheckBoxItems.ForEach(cbi => cbi.IsChecked = b);

    private void ResetChanges()
    {
        for (var index = 0; index < CheckBoxItems.Count; index++)
        {
            CheckBoxItems[index].IsChecked = _originalSelection[index];
        }
    }

    private async Task Save()
    {
        var pages = CheckBoxItems.Where(ti => ti.IsChecked)
            .Select(cbi => cbi.Page);

        await UserService.SetFonbecAuthClaim(UserId, pages);

        NavigationManager.NavigateTo(NavRoutes.Users);
    }
}