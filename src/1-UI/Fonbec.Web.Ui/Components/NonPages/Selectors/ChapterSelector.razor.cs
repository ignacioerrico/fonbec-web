using Fonbec.Web.Logic.Models;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.NonPages.Selectors;

public partial class ChapterSelector
{
    private bool _dataLoaded;

    private readonly List<SelectableModel<int>> _chapters = [];

    [Parameter]
    public int SelectedChapterId { get; set; }
    
    [Parameter]
    public EventCallback<int> SelectedChapterIdChanged { get; set; }

    /// <summary>
    /// Callback invoked when chapters are loaded. The int parameter indicates the number of chapters loaded.
    /// </summary>
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
            SelectedChapterId = chapters.First().Key;
            await OnSelectedValueChanged(SelectedChapterId);
        }

        await base.OnInitializedAsync();
    }

    private async Task OnSelectedValueChanged(int selectedChapterId)
    {
        await SelectedChapterIdChanged.InvokeAsync(selectedChapterId);
    }
}