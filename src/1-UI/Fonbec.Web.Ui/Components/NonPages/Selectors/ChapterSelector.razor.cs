using Fonbec.Web.Logic.Models;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.NonPages.Selectors;

public partial class ChapterSelector
{
    private bool _dataLoaded;

    private readonly List<SelectableModel<int>> _chapters = new();

    private int _selectedChapterId;

    [Parameter]
    public int SelectedChapterId { get; set; }
    
    [Parameter]
    public EventCallback<int> SelectedChapterIdChanged { get; set; }

    [Parameter]
    public EventCallback<int> OnChaptersLoaded { get; set; }

    [Inject]
    public IChapterService ChapterService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        var chapters = await ChapterService.GetAllChaptersForSelectionAsync();

        _dataLoaded = true;

        _chapters.AddRange(chapters);

        await OnChaptersLoaded.InvokeAsync(chapters.Count);

        if (chapters.Count > 0)
        {
            _selectedChapterId = chapters.First().Key;
            await OnSelectedValueChanged(_selectedChapterId);
        }

        await base.OnInitializedAsync();
    }

    private async Task OnSelectedValueChanged(int selectedChapterId)
    {
        SelectedChapterId = selectedChapterId;
        await SelectedChapterIdChanged.InvokeAsync(SelectedChapterId);
    }
}