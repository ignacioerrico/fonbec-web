using System.Runtime.CompilerServices;
using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models.Students;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Components.Pages;
using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.Pages.Students;

[PageMetadata(nameof(SponsorStudentsList), "Mis becarios", [FonbecRole.Uploader])]
public partial class SponsorStudentsList : AuthenticationRequiredComponentBase
{
    [Parameter] public int SponsorId { get; set; }

    [Inject] public IStudentService StudentService { get; set; } = null!;

    private List<SponsorStudentsListViewModel> _viewModels = [];

    private string _searchString = string.Empty;
    private bool _sortByLastName;
    private bool FilterStudents(SponsorStudentsListViewModel viewModel) =>
        string.IsNullOrWhiteSpace(_searchString)
        || $"{viewModel.StudentFirstName} {viewModel.StudentLastName}".ContainsIgnoringAccents(_searchString)
        || (!string.IsNullOrWhiteSpace(viewModel.StundentNickName)
            && $"{viewModel.StundentNickName} {viewModel.StudentLastName}".ContainsIgnoringAccents(_searchString));

    private string StudentFullName(SponsorStudentsListViewModel viewModel) =>
        _sortByLastName
            ? $"{viewModel.StudentLastName}, {viewModel.StudentFirstName}"
            : $"{viewModel.StudentFirstName} {viewModel.StudentLastName}";

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        Loading = true;
        _viewModels = await StudentService.GetStudentsBySponsorIdAsync(SponsorId);
        Loading = false;
    }
}