using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.Models.Chapters.Input;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Chapters;

[PageMetadata(nameof(CreateChapter), "Crear filial", [FonbecRole.Admin])]
public partial class CreateChapter : AuthenticationRequiredComponentBase
{
    private MudForm? _form;
    private string? _name;

    [Parameter]
    public EventCallback OnChapterCreated { get; set; }

    [Inject]
    public IChapterService ChapterService { get; set; } = null!;

    private bool IsCreateButtonDisabled => string.IsNullOrWhiteSpace(_name);

    private async Task CreateAChapter()
    {
        if (_form?.IsValid is null || _name is null)
        {
            return;
        }

        var inputModel = new CreateChapterInputModel(_name, FonbecClaim.UserId);
        await ChapterService.CreateChapterAsync(inputModel);
        await OnChapterCreated.InvokeAsync(); // Notufy parent component (ChaptersList)
        _name = string.Empty;
    }

    private void Cancel()
    {
        _name = string.Empty;
    }
}