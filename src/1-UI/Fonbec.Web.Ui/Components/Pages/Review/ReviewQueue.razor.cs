using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.Models.Documents;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Constants;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Review;

[PageMetadata(nameof(ReviewQueue), "Revisión de documentos", [FonbecRole.Reviewer, FonbecRole.Manager])]
public partial class ReviewQueue : AuthenticationRequiredComponentBase
{
    private ReviewProgressViewModel _progress = new();
    private bool _takingNext;

    [Inject]
    public IDocumentService DocumentService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        Loading = true;

        // A reviewer may only hold one document at a time. If they already have one in review
        // (and navigated away, closed the browser, or signed in elsewhere), resume it.
        var activeLockDocumentId = await DocumentService.GetActiveReviewLockAsync(FonbecClaim.UserId, UserRole);
        if (activeLockDocumentId is { } documentId)
        {
            Snackbar.Add("Este es el documento que estabas revisando.", Severity.Info);
            NavigationManager.NavigateTo(NavRoutes.ReviewDocument(documentId));
            return;
        }

        _progress = await DocumentService.GetGlobalReviewProgressAsync(FonbecClaim.UserId, UserRole, null);

        Loading = false;
    }

    private int Pending => _progress.PendingLetters + _progress.PendingReportCards + _progress.PendingOther;

    private async Task ReviewNextAsync()
    {
        _takingNext = true;

        var next = await DocumentService.TakeNextForReviewAsync(FonbecClaim.UserId, UserRole);

        if (next is null)
        {
            _takingNext = false;
            Snackbar.Add("No hay documentos pendientes.", Severity.Info);
            return;
        }

        NavigationManager.NavigateTo(NavRoutes.ReviewDocument(next.DocumentId));
    }
}