using Fonbec.Web.Logic.Models.Chapters;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.Pages.Chapters;

public partial class ChaptersList
{
    private List<AllChaptersViewModel> _chapters = [];

    [Inject]
    public IChapterService ChapterService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        _chapters = await ChapterService.GetAllChaptersAsync();
    }

    public async Task RefreshChapters()
    {
        _chapters = await ChapterService.GetAllChaptersAsync();
    }
}