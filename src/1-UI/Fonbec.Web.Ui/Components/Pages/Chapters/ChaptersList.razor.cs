using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models.Chapters;
using Fonbec.Web.Logic.Models.Chapters.Input;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Chapters;

[PageMetadata(nameof(ChaptersList), "Lista de filiales", [FonbecRole.Admin])]
public partial class ChaptersList : AuthenticationRequiredComponentBase
{
    private List<ChaptersListViewModel> _viewModels = [];

    private ChaptersListViewModel _originalViewModel = new();

    private string _searchString = string.Empty;

    [Inject]
    public IChapterService ChapterService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        Loading = true;

        _viewModels = await ChapterService.GetAllChaptersAsync();

        Loading = false;
    }

    /// <summary>
    /// Called by MudTable to determine if a row should be displayed
    /// </summary>
    /// <param name="viewModel">The chapter view model to evaluate against the search string. Cannot be null.</param>
    /// <returns>true if the chapter matches the search string in any of the relevant fields; otherwise, false.</returns>
    private bool Filter(ChaptersListViewModel viewModel) =>
        string.IsNullOrWhiteSpace(_searchString)
        || viewModel.ChapterName.ContainsIgnoringAccents(_searchString);

    private void StartedEditingItem(ChaptersListViewModel originalViewModel) =>
        _originalViewModel = originalViewModel.DeepClone();

    private async Task CommittedItemChangesAsync(ChaptersListViewModel modifiedViewModel)
    {
        if (_originalViewModel.IsEqualTo(modifiedViewModel))
        {
            Snackbar.Add("No se realizaron cambios.", Severity.Info);
            return;
        }

        var updateChapterInputModel = new UpdateChapterInputModel(
            modifiedViewModel.ChapterId,
            modifiedViewModel.ChapterName,
            modifiedViewModel.Notes,
            FonbecClaim.UserId
        );

        Loading = true;

        var result = await ChapterService.UpdateChapterAsync(updateChapterInputModel);
        
        Loading = false;
        
        if (!result.AnyAffectedRows)
        {
            Snackbar.Add("No se pudo actualizar la filial.", Severity.Error);
        }

        _viewModels.Single(vm => vm.ChapterId == modifiedViewModel.ChapterId).LastUpdatedOnUtc = DateTime.Now;
    }
}