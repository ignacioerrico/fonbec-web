using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.Models.Chapters;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.Pages.Chapters;

[PageMetadata(nameof(ChaptersList), "Lista de filiales", [FonbecRole.Admin])]
public partial class ChaptersList
{
    private List<ChaptersListViewModel> _viewModel = [];

    [Inject]
    public IChapterService ChapterService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        _viewModel = await ChapterService.GetAllChaptersAsync();
    }

    public async Task RefreshChapters()
    {
        _viewModel = await ChapterService.GetAllChaptersAsync();
    }
}