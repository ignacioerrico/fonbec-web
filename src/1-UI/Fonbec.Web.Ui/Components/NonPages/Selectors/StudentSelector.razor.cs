using Fonbec.Web.Logic.Models;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.NonPages.Selectors;

public partial class StudentSelector
{
    private readonly List<SelectableModel<int>> _students = [];

    private bool _loading;

    [Parameter]
    public int? ChapterId { get; set; }

    [Parameter]
    public bool SelectFirstItemOnLoad { get; set; }

    [Parameter]
    public int SelectedStudentId { get; set; }

    [Parameter]
    public EventCallback<int> SelectedStudentIdChanged { get; set; }

    /// <summary>
    /// Callback invoked when students are loaded. The int parameter indicates the number of students loaded.
    /// </summary>
    [Parameter]
    public EventCallback<int> NumberOfStudentsLoaded { get; set; }

    [Inject]
    public IStudentService StudentService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        _loading = true;

        var students = await StudentService.GetAllStudentsForSelectionAsync(ChapterId);

        _loading = false;

        _students.AddRange(students);

        await NumberOfStudentsLoaded.InvokeAsync(students.Count);

        if (SelectFirstItemOnLoad && students.Count > 0)
        {
            if (SelectedStudentId == 0)
            {
                SelectedStudentId = students.First().Key;
            }

            await OnSelectedValueChanged(SelectedStudentId);
        }

        await base.OnInitializedAsync();
    }

    private async Task<IEnumerable<int>> Search(string value, CancellationToken token)
    {
        var result = string.IsNullOrEmpty(value)
            ? _students.Select(c => c.Key)
            : _students.Where(c => c.DisplayName.Contains(value, StringComparison.OrdinalIgnoreCase))
                           .Select(c => c.Key);

        return await Task.FromResult(result);
    }

    private async Task OnSelectedValueChanged(int selectedSponsorId) =>
        await SelectedStudentIdChanged.InvokeAsync(selectedSponsorId);
    
    private string? MapKeyToDisplayName(int key) =>
        _students.FirstOrDefault(s => s.Key == key)?.DisplayName;
}