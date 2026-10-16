using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.DataModels.Chapters;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.Email;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Components.Layout;
using Fonbec.Web.Ui.Constants;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Chapters;

[PageMetadata(nameof(ChapterSettings), "Configuración de la filial", [FonbecRole.Manager])]
public partial class ChapterSettings : AuthenticationRequiredComponentBase
{
    private DocumentRejectionBehavior? _behavior;
    private ChapterEmailSettingsEditor? _emailSettings;
    private bool _saving;
    private bool _savingMessages;

    [Inject]
    public IDocumentService DocumentService { get; set; } = null!;

    [Inject]
    public IChapterEmailSettingsService ChapterEmailSettingsService { get; set; } = null!;

    [Inject]
    public NavMenuRefresh NavMenuRefresh { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (FonbecClaim.ChapterId is not int chapterId)
        {
            Snackbar.Add("El coordinador no tiene una filial asignada.", Severity.Error);
            NavigationManager.NavigateTo(NavRoutes.Home);
            return;
        }

        Loading = true;
        _behavior = await DocumentService.GetChapterRejectionBehaviorAsync(chapterId, UserRole);
        _emailSettings = await ChapterEmailSettingsService.GetEditorAsync(chapterId);
        Loading = false;

        if (_behavior is null)
        {
            Snackbar.Add(DocumentMessages.ChapterNotFound, Severity.Error);
            NavigationManager.NavigateTo(NavRoutes.Home);
        }
    }

    private async Task OnBehaviorChanged(DocumentRejectionBehavior behavior)
    {
        if (_behavior is null || _saving || behavior == _behavior || FonbecClaim.ChapterId is not int chapterId)
        {
            return;
        }

        _saving = true;
        var result = await DocumentService.SetChapterRejectionBehaviorAsync(
            chapterId, UserRole, FonbecClaim.UserId, behavior);
        _saving = false;

        if (result.Errors is { Count: > 0 })
        {
            Snackbar.Add(result.Errors[0], Severity.Error);
            return;
        }

        _behavior = behavior;
        NavMenuRefresh.NotifyChanged();
        Snackbar.Add("Se guardó el comportamiento.", Severity.Success);
    }

    private void RestoreDefault(ChapterEmailTemplateEditor message)
    {
        message.Subject = message.DefaultSubject;
        message.Body = message.DefaultBody;
    }

    private async Task SaveMessagesAsync()
    {
        if (_emailSettings is null || _savingMessages || FonbecClaim.ChapterId is not int chapterId)
        {
            return;
        }

        _savingMessages = true;
        var templates = _emailSettings.Messages
            .Select(message => new ChapterEmailTemplateDataModel
            {
                Kind = message.Kind,
                Subject = message.Subject,
                Body = message.Body,
            })
            .ToList();
        var errors = await ChapterEmailSettingsService.SaveAsync(
            chapterId,
            FonbecClaim.UserId,
            _emailSettings.SubjectPrefix,
            templates);
        _savingMessages = false;

        if (errors.Count > 0)
        {
            foreach (var error in errors)
            {
                Snackbar.Add(error, Severity.Error);
            }

            return;
        }

        Snackbar.Add("Se guardaron los mensajes.", Severity.Success);
    }
}