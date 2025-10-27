using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.Models.Chapters.Input;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Constants;
using Fonbec.Web.Ui.Models.Chapter;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Chapters;

[PageMetadata(nameof(ChapterCreate), "Crear y actualizar filial", [FonbecRole.Admin])]
public partial class ChapterCreate : AuthenticationRequiredComponentBase
{
    private readonly ChapterCreateBindModel _bindModel = new();

    private bool _formValidationSucceeded;

    private MudTextField<string> _mudTextFieldName = null!;

    private bool _saving;

    private bool SaveButtonDisabled => Loading
                                       || _saving
                                       || !_formValidationSucceeded;

    [Inject]
    public IChapterService ChapterService { get; set; } = null!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await _mudTextFieldName.FocusAsync();
        }

        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task OnKeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !SaveButtonDisabled)
        {
            await Save();
        }
    }

    private async Task Save()
    {
        _saving = true;

        var createChapterInputModel = new CreateChapterInputModel(
            _bindModel.ChapterName,
            _bindModel.ChapterNotes,
            FonbecClaim.UserId);

        var result = await ChapterService.CreateChapterAsync(createChapterInputModel);
        if (!result.AnyAffectedRows)
        {
            Snackbar.Add("No se pudo crear el becario.", Severity.Error);
        }

        _saving = false;

        NavigationManager.NavigateTo(NavRoutes.Chapters);
    }
}