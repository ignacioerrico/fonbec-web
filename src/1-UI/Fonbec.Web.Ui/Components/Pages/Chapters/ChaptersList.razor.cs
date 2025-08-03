using Fonbec.Web.Logic.Services;
using Fonbec.Web.Logic.ViewModels.Chapters;
using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.Pages.Chapters;

public partial class ChaptersList
{
    private List<ChaptersListViewModel> _chapters = new();

    [Inject]
    public IChaptersListService ChaptersListService { get; set; } = null!;

    protected override void OnInitialized()
    {
        _chapters = ChaptersListService.GetAllChapters().ToList();
    }
}