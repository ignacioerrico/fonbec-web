using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities.Enums;
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
    private bool _saving;

    [Inject]
    public IDocumentService DocumentService { get; set; } = null!;

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
}