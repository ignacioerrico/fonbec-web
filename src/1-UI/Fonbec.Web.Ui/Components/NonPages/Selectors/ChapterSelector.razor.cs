using Fonbec.Web.Logic.Models;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.NonPages.Selectors;

public partial class ChapterSelector
{
    private bool _dataLoaded;

    private readonly List<SelectableModel<int>> _chapters = new();

    private SelectableModel<int> _selectedChapter = null!;

    [Parameter]
    public int SelectedChapterId { get; set; }
    
    [Parameter]
    public EventCallback<int> SelectedChapterIdChanged { get; set; }

    [Inject]
    public IChapterService ChapterService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        var chapters = await ChapterService.GetAllChaptersForSelectionAsync();

        _dataLoaded = true;

        _chapters.AddRange(chapters);

        if (chapters.Count > 0)
        {
            _selectedChapter = chapters.First();
            await OnSelectedValueChanged(_selectedChapter);
        }

        await base.OnInitializedAsync();
    }

    private async Task OnSelectedValueChanged(SelectableModel<int> selectedChapter)
    {
        SelectedChapterId = selectedChapter.Key;
        await SelectedChapterIdChanged.InvokeAsync(SelectedChapterId);
    }
}