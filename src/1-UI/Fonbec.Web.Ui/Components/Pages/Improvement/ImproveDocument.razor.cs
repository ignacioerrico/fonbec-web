using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.Models.Documents;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Constants;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Improvement;

public partial class ImproveDocument : AuthenticationRequiredComponentBase
{
    private ImprovementWorkspaceViewModel? _workspace;
    private bool _expired;

    [Parameter]
    public long DocumentId { get; set; }

    [Inject]
    public IDocumentService DocumentService { get; set; } = null!;

    [Inject]
    public IDialogService DialogService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        Loading = true;
        _workspace = await DocumentService.GetImprovementWorkspaceAsync(DocumentId, FonbecClaim.UserId, UserRole);
        Loading = false;

        if (_workspace is null)
        {
            Snackbar.Add("El documento ya no está disponible para mejora.", Severity.Warning);
            NavigationManager.NavigateTo(NavRoutes.ImprovementQueue);
        }
    }

    private string PageUrl(int pageNumber) => NavRoutes.ImproveDocumentPage(DocumentId, pageNumber);

    private async Task ReleaseAsync()
    {
        await DocumentService.ReleaseImprovementLockAsync(DocumentId, FonbecClaim.UserId);
        Snackbar.Add("Documento liberado.", Severity.Info);
        NavigationManager.NavigateTo(NavRoutes.ImprovementQueue);
    }

    private Task OnImprovementCompleted()
    {
        NavigationManager.NavigateTo(NavRoutes.ImprovementQueue);
        return Task.CompletedTask;
    }

    private async Task OnCountdownExpired()
    {
        _expired = true;
        await InvokeAsync(StateHasChanged);

        await DocumentService.ReleaseImprovementLockAsync(DocumentId, FonbecClaim.UserId);

        await DialogService.ShowMessageBox(
            "Se te terminó el tiempo",
            "Se te terminó el tiempo para mejorar este documento. El documento volvió a la cola.",
            yesText: "Aceptar");

        NavigationManager.NavigateTo(NavRoutes.ImprovementQueue);
    }

    private static string DocumentTypeLabel(DocumentType documentType) => documentType switch
    {
        DocumentType.Letter => "Carta",
        DocumentType.ReportCard => "Boletín",
        DocumentType.Other => "Otro documento",
        _ => string.Empty,
    };
}