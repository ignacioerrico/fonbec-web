using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models.Facilitators;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.Pages.Facilitators;

[PageMetadata(nameof(FacilitatorStudentsList), "Mis becarios", [FonbecRole.Uploader])]
public partial class FacilitatorStudentsList : AuthenticationRequiredComponentBase
{
    [Inject] public IFacilitatorService FacilitatorService { get; set; } = null!;

    private List<MisBecariosRowViewModel> _students = [];

    private string _searchString = string.Empty;
    private bool _sortByLastName;

    private bool FilterStudents(MisBecariosRowViewModel viewModel) =>
        string.IsNullOrWhiteSpace(_searchString)
        || $"{viewModel.StudentFirstName} {viewModel.StudentLastName}".ContainsIgnoringAccents(_searchString)
        || (!string.IsNullOrEmpty(viewModel.StudentNickName)
            && $"{viewModel.StudentNickName} {viewModel.StudentLastName}".ContainsIgnoringAccents(_searchString));

    private string StudentFullName(MisBecariosRowViewModel viewModel) =>
        _sortByLastName
            ? $"{viewModel.StudentLastName}, {viewModel.StudentFirstName}"
            : $"{viewModel.StudentFirstName} {viewModel.StudentLastName}";

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        Loading = true;
        var dashboard = await FacilitatorService.GetMisBecariosDashboardAsync(FonbecClaim.UserId);
        _students = dashboard.Students;
        Loading = false;
    }
}
