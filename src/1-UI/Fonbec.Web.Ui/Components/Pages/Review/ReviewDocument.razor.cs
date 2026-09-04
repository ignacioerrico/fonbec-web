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
        _workspace = await DocumentService.GetReviewWorkspaceAsync(DocumentId, FonbecClaim.UserId, UserRole);
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

    private Task OnReviewCompleted()
    {
        NavigationManager.NavigateTo(NavRoutes.ReviewQueue);
        return Task.CompletedTask;
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