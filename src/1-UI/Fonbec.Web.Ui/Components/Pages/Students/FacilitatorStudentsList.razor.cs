using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models.Students;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.Pages.Students;

[PageMetadata(nameof(FacilitatorStudentsList), "Mis becarios", [FonbecRole.Uploader])]
public partial class FacilitatorStudentsList : AuthenticationRequiredComponentBase
{
    [Inject] public IStudentService StudentService { get; set; } = null!;

    private List<FacilitatorStudentsListViewModel> _viewModels = [];

    private string _searchString = string.Empty;
    private bool _sortByLastName;
    private bool FilterStudents(FacilitatorStudentsListViewModel viewModel) =>
        string.IsNullOrWhiteSpace(_searchString)
        || $"{viewModel.StudentFirstName} {viewModel.StudentLastName}".ContainsIgnoringAccents(_searchString);


    private string StudentFullName(FacilitatorStudentsListViewModel viewModel) =>
        _sortByLastName
            ? $"{viewModel.StudentLastName}, {viewModel.StudentFirstName}"
            : $"{viewModel.StudentFirstName} {viewModel.StudentLastName}";

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        Loading = true;
        _viewModels = await StudentService.GetStudentsByFacilitatorIdAsync(FonbecClaim.UserId);
        Loading = false;
    }
}