using Fonbec.Web.DataAccess.Constants;
using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.NonPages.Selectors;

public partial class RoleSelector
{
    [Parameter]
    public string SelectedRole { get; set; } = FonbecRole.Manager;

    [Parameter]
    public EventCallback<string> SelectedRoleChanged { get; set; }

    private async Task OnSelectedValueChanged(string selectedRole)
    {
        await SelectedRoleChanged.InvokeAsync(selectedRole);
    }
}