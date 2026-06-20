using Fonbec.Web.Logic.Models;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.NonPages.Selectors;

public partial class ChapterSelector
{
    private readonly List<SelectableModel<int>> _chapters = [];

    private bool _loading;

    [Parameter]
    public int SelectedChapterId { get; set; }

    [Parameter]
    public bool SelectFirstItemOnLoad { get; set; }

    [Parameter]
    public EventCallback<int> SelectedChapterIdChanged { get; set; }

    /// <summary>
    /// Callback invoked when chapters are loaded. The int parameter indicates the number of chapters loaded.
    /// </summary>
    [Parameter]
    public EventCallback<int> NumberOfChaptersLoaded { get; set; }

    [Inject]
    public IChapterService ChapterService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        _loading = true;
        
        var chapters = await ChapterService.GetAllChaptersForSelectionAsync();

        _loading = false;

        _chapters.AddRange(chapters);

        await NumberOfChaptersLoaded.InvokeAsync(chapters.Count);

        if (SelectFirstItemOnLoad && chapters.Count > 0)
        {
            SelectedChapterId = chapters.First().Key;
            await OnSelectedValueChanged(SelectedChapterId);
        }

        await base.OnInitializedAsync();
    }

    private async Task<IEnumerable<int>> Search(string value, CancellationToken token)
    {
        var result = string.IsNullOrEmpty(value)
            ? _chapters.Select(c => c.Key)
            : _chapters.Where(c => c.DisplayName.Contains(value, StringComparison.OrdinalIgnoreCase))
                           .Select(c => c.Key);

        return await Task.FromResult(result);
    }

    private async Task OnSelectedValueChanged(int selectedChapterId) =>
        await SelectedChapterIdChanged.InvokeAsync(selectedChapterId);

    private string? MapKeyToDisplayName(int key) =>
        _chapters.FirstOrDefault(s => s.Key == key)?.DisplayName;
}