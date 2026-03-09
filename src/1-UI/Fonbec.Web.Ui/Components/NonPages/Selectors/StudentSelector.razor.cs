using Fonbec.Web.Logic.Models;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.NonPages.Selectors;

public partial class StudentSelector
{
    private readonly List<SelectableModel<int>> _students = [];

    private bool _dataLoaded;

    [Parameter]
    public int? ChapterId { get; set; }

    [Parameter]
    public int SelectedStudentId { get; set; }

    [Parameter]
    public EventCallback<int> SelectedStudentIdChanged { get; set; }

    /// <summary>
    /// Callback invoked when students are loaded. The int parameter indicates the number of students loaded.
    /// </summary>
    [Parameter]
    public EventCallback<int> OnStudentsLoaded { get; set; }

    [Inject]
    public IStudentService StudentService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        _dataLoaded = false;

        var students = await StudentService.GetAllStudentsForSelectionAsync(ChapterId);

        _dataLoaded = true;

        _students.AddRange(students);

        await OnStudentsLoaded.InvokeAsync(students.Count);

        if (students.Count > 0)
        {
            if (SelectedStudentId == 0)
            {
                SelectedStudentId = students.First().Key;
            }

            await OnSelectedValueChanged(SelectedStudentId);
        }

        await base.OnInitializedAsync();
    }

    private async Task OnSelectedValueChanged(int selectedSponsorId) =>
        await SelectedStudentIdChanged.InvokeAsync(selectedSponsorId);
    
    private string? MapKeyToDisplayName(int key) =>
        _students.FirstOrDefault(s => s.Key == key)?.DisplayName;
}