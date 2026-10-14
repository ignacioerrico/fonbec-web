using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.Models.Documents;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Constants;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Review;

[PageMetadata(nameof(ReviewDocument), "Revisar documento", [FonbecRole.Reviewer, FonbecRole.Manager])]
public partial class ReviewDocument : AuthenticationRequiredComponentBase
{
    private ReviewWorkspaceViewModel? _workspace;
    private bool _expired;
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
        _workspace = null;
        await InvokeAsync(StateHasChanged);

        var workspace = await DocumentService.GetReviewWorkspaceAsync(documentId, FonbecClaim.UserId, UserRole);

        if (version != _loadVersion)
        {
            return;
        }

        _workspace = workspace;
        Loading = false;

        if (_workspace is null)
        {
            Snackbar.Add("El documento ya no está disponible para revisión.", Severity.Warning);
            NavigationManager.NavigateTo(NavRoutes.ReviewQueue);
        }
    }

    private async Task ReleaseAsync()
    {
        await DocumentService.ReleaseReviewLockAsync(DocumentId, FonbecClaim.UserId);
        Snackbar.Add("Documento liberado.", Severity.Info);
        NavigationManager.NavigateTo(NavRoutes.ReviewQueue);
    }

    private async Task OnReviewCompleted()
    {
        var progress = await DocumentService.GetGlobalReviewProgressAsync(FonbecClaim.UserId, UserRole, null);
        var pending = progress.PendingLetters + progress.PendingReportCards + progress.PendingOther;

        if (pending > 0)
        {
            var continueReviewing = await DialogService.ShowMessageBox(
                "Revisión completada",
                "¿Querés revisar el siguiente documento de la cola?",
                yesText: "Revisar siguiente",
                cancelText: "Volver a la cola");

            if (continueReviewing == true)
            {
                var next = await DocumentService.TakeNextForReviewAsync(FonbecClaim.UserId, UserRole);
                if (next is not null)
                {
                    NavigationManager.NavigateTo(NavRoutes.ReviewDocument(next.DocumentId));
                    return;
                }

                Snackbar.Add("No hay documentos pendientes.", Severity.Info);
            }
        }

        NavigationManager.NavigateTo(NavRoutes.ReviewQueue);
    }

    private async Task OnCountdownExpired()
    {
        _expired = true;
        await InvokeAsync(StateHasChanged);

        // The lock has elapsed: release it now so the document returns to the queue immediately,
        // rather than lingering as locked until the next take-next sweep. The server only clears the
        // lock if this reviewer still holds it, so it safely no-ops if another reviewer already took it.
        await DocumentService.ReleaseReviewLockAsync(DocumentId, FonbecClaim.UserId);

        await DialogService.ShowMessageBox(
            "Se te terminó el tiempo",
            "Se te terminó el tiempo para revisar este documento. El documento volvió a la cola.",
            yesText: "Aceptar");

        NavigationManager.NavigateTo(NavRoutes.ReviewQueue);
    }

    private static string DocumentTypeLabel(DocumentType documentType) => documentType switch
    {
        DocumentType.Letter => "Carta",
        DocumentType.ReportCard => "Boletín",
        DocumentType.Other => "Otro documento",
        _ => string.Empty,
    };
}