using MudBlazor;
using Microsoft.AspNetCore.Components;
using Fonbec.Web.Logic.Models.Chapters.Input;
using Fonbec.Web.Logic.Services;

namespace Fonbec.Web.Ui.Components.Pages.Chapters;

public partial class CreateChapter : AuthenticationRequiredComponentBase
{
    private MudForm? _form;
    private string? _name;

    [Inject]
    public IChapterService ChapterService { get; set; } = null!;

    [Parameter]
    public EventCallback OnChapterCreated { get; set; }

    private bool IsCreateButtonDisabled => string.IsNullOrWhiteSpace(_name);

    private async Task CreateAChapter()
    {
        if (_form?.IsValid is null || _name is null)
        {
            return;
        }

        var userId = await GetAuthenticatedUserIdAsync();
        var inputModel = new CreateChapterInputModel(_name, userId);
        await ChapterService.CreateChapterAsync(inputModel);
        await OnChapterCreated.InvokeAsync(); // Notufy parent component (ChaptersList)
        _name = string.Empty;
    }

    private void Cancel()
    {
        _name = string.Empty;
    }
}