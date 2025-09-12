using Fonbec.Web.DataAccess.Constants;
using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.NonPages;

public partial class RolesSelector
{
    private IEnumerable<string> _selectedRoles = null!;

    [Parameter]
    public bool? Required { get; set; }

    [Parameter]
    public List<string>? SelectedRoles { get; set; }

    [Parameter]
    public EventCallback<List<string>> SelectedRolesChanged { get; set; }

    private async Task OnSelectedValuesChanged(IEnumerable<string?>? selectedRoles)
    {
        _selectedRoles = selectedRoles!;
        var roles = _selectedRoles.ToList();
        await SelectedRolesChanged.InvokeAsync(roles);
    }

    protected override async Task OnParametersSetAsync()
    {
        _selectedRoles = SelectedRoles?.Where(selectedRole => FonbecRole.AllRoles.Contains(selectedRole))
                         ?? [];

        await base.OnParametersSetAsync();
    }
}