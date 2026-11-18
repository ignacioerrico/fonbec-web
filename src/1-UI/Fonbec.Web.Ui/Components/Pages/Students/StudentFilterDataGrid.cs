using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Students;

public sealed record StudentColumnFilter(string Column, string Operator, string? Value);

[CascadingTypeParameter(nameof(T))]
public sealed class StudentFilterDataGrid<T> : MudDataGrid<T>
{
    private StudentColumnFilter[] _lastSavedFilters = [];

    [Parameter]
    public StudentColumnFilter[] SavedFilters { get; set; } = [];

    [Parameter]
    public EventCallback<StudentColumnFilter[]> SavedFiltersChanged { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            foreach (var saved in SavedFilters)
            {
                var column = RenderedColumns.FirstOrDefault(column => column.Tag is string tag && tag == saved.Column);
                if (column is null)
                {
                    continue;
                }

                var definition = column.FilterContext.FilterDefinition!;
                definition.Operator = saved.Operator;
                definition.Value = saved.Value;
                FilterDefinitions.Add(definition);
            }

            _lastSavedFilters = GetActiveFilters();
            if (FilterDefinitions.Count > 0)
            {
                StateHasChanged();
            }

            return;
        }

        // MudBlazor 8.15 updates its native column filters internally and rerenders the grid.
        var activeFilters = GetActiveFilters();
        if (!_lastSavedFilters.SequenceEqual(activeFilters))
        {
            _lastSavedFilters = activeFilters;
            await SavedFiltersChanged.InvokeAsync(activeFilters);
        }
    }

    private StudentColumnFilter[] GetActiveFilters() => FilterDefinitions
        .Where(filter => filter.Column?.Tag is string && filter.Operator is not null
            && (filter.Operator is FilterOperator.String.Empty or FilterOperator.String.NotEmpty || filter.Value is string { Length: > 0 }))
        .Select(filter => new StudentColumnFilter((string)filter.Column!.Tag!, filter.Operator!, filter.Value as string))
        .OrderBy(filter => filter.Column, StringComparer.Ordinal)
        .ToArray();
}
