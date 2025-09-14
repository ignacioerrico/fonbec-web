using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Logic.ViewModels.Chapters;
using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.Pages.Chapters;

public partial class ChaptersList
{
    private List<ChaptersListViewModel> _chapters = [];
    private EditChapter _editChapter;
    private EditChapterInputModel _chapterToEdit = new();

    [Inject]
    public IChaptersListService ChaptersListService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        _chapters = await ChaptersListService.GetAllChaptersAsync();
    }
    public async Task RefreshChapters()
    {
        _chapters = await ChaptersListService.GetAllChaptersAsync();
    }

    private void EditChapter(ChaptersListViewModel chapter)
    {
        _chapterToEdit.ChapterID = 1;
        _chapterToEdit.ChapterName = chapter.ChapterName;
    }
}