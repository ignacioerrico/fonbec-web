using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.NonPages;

public partial class FilterByList<T>
{
    private string _filterIcon = Icons.Material.Outlined.FilterAlt;
    private bool _isFilterOpen;
    private bool _areAllItemsSelected = true;
    private HashSet<string> _selectedItems = [];
    private HashSet<string> _selectedItemsBeforeFilter = [];
    private FilterDefinition<T> _filterItemsDefinition = null!;

    [Parameter, EditorRequired]
    public IEnumerable<string> AllItems { get; set; } = null!;

    [Parameter, EditorRequired]
    public FilterContext<T> FilterContext { get; set; } = null!;

    [Parameter]
    public Func<T, string> FilterBy { get; set; } = null!;

    [Parameter]
    public Func<string, string> TranslateUsing { get; set; } = s => s;

    [Parameter]
    public string[]? AppliedItems { get; set; }

    [Parameter]
    public EventCallback<string[]?> AppliedItemsChanged { get; set; }

    private string FilterIcon => AppliedItemsChanged.HasDelegate
        ? AppliedItems is null ? Icons.Material.Outlined.FilterAlt : Icons.Material.Filled.FilterAlt
        : _filterIcon;

    protected override void OnInitialized()
    {
        _selectedItems = AllItems.ToHashSet();
        _selectedItemsBeforeFilter = _selectedItems.ToHashSet();
        _filterItemsDefinition = new FilterDefinition<T>
        {
            FilterFunction = vm => _selectedItemsBeforeFilter.Contains(FilterBy(vm))
        };

        base.OnInitialized();
    }

    private void OpenFilter()
    {
        if (AppliedItemsChanged.HasDelegate)
        {
            _selectedItems = AppliedItems?.ToHashSet() ?? AllItems.ToHashSet();
            _areAllItemsSelected = AppliedItems is null;
        }

        _isFilterOpen = true;
    }

    private void OnSelectAllItemsChanged(bool areAllChaptersSelected)
    {
        _areAllItemsSelected = areAllChaptersSelected;

        if (areAllChaptersSelected)
        {
            _selectedItems = AllItems.ToHashSet();
        }
        else
        {
            _selectedItems.Clear();
        }
    }

    private void SelectedItemChanged(string chapter, bool isSelected)
    {
        if (isSelected)
        {
            _selectedItems.Add(chapter);
        }
        else
        {
            _selectedItems.Remove(chapter);
        }

        _areAllItemsSelected = _selectedItems.Count == AllItems.Count();
    }

    private async Task ClearItemsFilterAsync(FilterContext<T> context)
    {
        _areAllItemsSelected = true;
        _selectedItems = AllItems.ToHashSet();
        _selectedItemsBeforeFilter = _selectedItems.ToHashSet();
        _filterIcon = Icons.Material.Outlined.FilterAlt;
        if (AppliedItemsChanged.HasDelegate)
        {
            await AppliedItemsChanged.InvokeAsync(null);
        }
        else
        {
            await context.Actions.ClearFilterAsync(_filterItemsDefinition);
        }
        _isFilterOpen = false;
    }

    private async Task ApplyItemsFilterAsync(FilterContext<T> context)
    {
        _selectedItemsBeforeFilter = _selectedItems.ToHashSet();
        _filterIcon = _selectedItemsBeforeFilter.Count == AllItems.Count()
            ? Icons.Material.Outlined.FilterAlt
            : Icons.Material.Filled.FilterAlt;
        if (AppliedItemsChanged.HasDelegate)
        {
            await AppliedItemsChanged.InvokeAsync(_areAllItemsSelected ? null : _selectedItems.ToArray());
        }
        else
        {
            await context.Actions.ApplyFilterAsync(_filterItemsDefinition);
        }
        _isFilterOpen = false;
    }
}
