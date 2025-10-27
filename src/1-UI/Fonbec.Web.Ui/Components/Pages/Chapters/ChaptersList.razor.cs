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

    private string _searchString = string.Empty;

    [Inject]
    public IChapterService ChapterService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        _viewModels = await ChapterService.GetAllChaptersAsync();
    }

    /// <summary>
    /// Called by MudTable to determine if a row should be displayed
    /// </summary>
    /// <param name="viewModel">The chapter view model to evaluate against the search string. Cannot be null.</param>
    /// <returns>true if the chapter matches the search string in any of the relevant fields; otherwise, false.</returns>
    private bool Filter(ChaptersListViewModel viewModel) =>
        string.IsNullOrWhiteSpace(_searchString)
        || viewModel.ChapterName.ContainsIgnoringAccents(_searchString);

    private async Task CommittedItemChangesAsync(ChaptersListViewModel viewModel)
    {
        var updateChapterInputModel = new UpdateChapterInputModel(
            viewModel.ChapterId,
            viewModel.ChapterName,
            viewModel.Notes
        );

        var result = await ChapterService.UpdateChapterAsync(updateChapterInputModel);
        if (!result.AnyAffectedRows)
        {
            Snackbar.Add("No se pudo actualizar la filial.", Severity.Error);
        }

        _viewModels.Single(vm => vm.ChapterId == viewModel.ChapterId).LastUpdatedOnUtc = DateTime.Now;
    }
}