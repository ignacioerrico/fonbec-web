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
    private int _focusedPageNumber = 1;
    private long? _loadedDocumentId;
    private int _loadVersion;

    [Parameter]
    public long DocumentId { get; set; }

    [Inject]
    public IDocumentService DocumentService { get; set; } = null!;

    [Inject]
    public IDialogService DialogService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await LoadWorkspaceAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        if (_loadedDocumentId != DocumentId)
        {
            await LoadWorkspaceAsync();
        }
    }

    private async Task LoadWorkspaceAsync()
    {
        var documentId = DocumentId;
        var version = ++_loadVersion;
        _loadedDocumentId = documentId;
        Loading = true;
        _expired = false;
        _focusedPageNumber = 1;
        _workspace = null;
        await InvokeAsync(StateHasChanged);

        var workspace = await DocumentService.GetImprovementWorkspaceAsync(documentId, FonbecClaim.UserId, UserRole);

        if (version != _loadVersion)
        {
            return;
        }

        _workspace = workspace;
        Loading = false;

        if (_workspace is null)
        {
            Snackbar.Add("El documento ya no está disponible para mejora.", Severity.Warning);
            NavigationManager.NavigateTo(NavRoutes.ImprovementQueue);
        }
    }

    private string PageUrl(int pageNumber) => NavRoutes.ImproveDocumentPage(DocumentId, pageNumber);

    private void OnPageFocused(int pageNumber) => _focusedPageNumber = pageNumber;

    private async Task ReleaseAsync()
    {
        await DocumentService.ReleaseImprovementLockAsync(DocumentId, FonbecClaim.UserId);
        Snackbar.Add("Documento liberado.", Severity.Info);
        NavigationManager.NavigateTo(NavRoutes.ImprovementQueue);
    }

    private async Task OnImprovementCompleted()
    {
        var progress = await DocumentService.GetGlobalReviewProgressAsync(FonbecClaim.UserId, UserRole, null);

        if (progress.PendingImprovement > 0)
        {
            var continueImproving = await DialogService.ShowMessageBox(
                "Mejora completada",
                "¿Querés mejorar la siguiente imagen de la cola?",
                yesText: "Mejorar siguiente",
                cancelText: "Volver a la cola");

            if (continueImproving == true)
            {
                var next = await DocumentService.TakeNextForDigitalImprovementAsync(FonbecClaim.UserId, UserRole);
                if (next is not null)
                {
                    NavigationManager.NavigateTo(NavRoutes.ImproveDocument(next.DocumentId));
                    return;
                }

                Snackbar.Add("No hay imágenes pendientes de mejora.", Severity.Info);
            }
        }

        NavigationManager.NavigateTo(NavRoutes.ImprovementQueue);
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
