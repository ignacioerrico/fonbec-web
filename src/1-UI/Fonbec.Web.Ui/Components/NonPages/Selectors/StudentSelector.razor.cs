using Fonbec.Web.Logic.Models;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.NonPages.Selectors;
public partial class StudentSelector
{
    private bool _dataLoaded;

    private readonly List<SelectableModel<int>> _students = [];

    [Parameter]
    public int SelectedStudentId { get; set; }

    [Parameter]
    public EventCallback<int> SelectedStudentIdChanged { get; set; }

    /// <summary>
    /// Callback invoked when chapters are loaded. The int parameter indicates the number of chapters loaded.
    /// </summary>
    [Parameter]
    public EventCallback<int> OnStudentsLoaded { get; set; }

    [Inject]
    public IStudentService StudentService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        var students = await StudentService.GetAllStudentsForSelectionAsync();

        _dataLoaded = true;

        _students.AddRange(students);

        await OnStudentsLoaded.InvokeAsync(students.Count);

        if (students.Count > 0)
        {
            SelectedStudentId = students.First().Key;
            await OnSelectedValueChanged(SelectedStudentId);
        }

        await base.OnInitializedAsync();
    }

    private async Task OnSelectedValueChanged(int selectedStudentId)
    {
        await SelectedStudentIdChanged.InvokeAsync(selectedStudentId);
    }
}