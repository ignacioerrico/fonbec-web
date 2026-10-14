using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models.Documents;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Constants;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Review;

[PageMetadata(nameof(QueueContents), "Colas", [FonbecRole.Manager])]
public partial class QueueContents : AuthenticationRequiredComponentBase
{
    private List<ManagerQueueEntryViewModel> _reviewQueue = [];
    private List<ManagerQueueEntryViewModel> _improvementQueue = [];
    private long? _takingDocumentId;
    private bool _canImprove;

    [Inject]
    public IDocumentService DocumentService { get; set; } = null!;

    [Inject]
    public IUserService UserService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        Loading = true;

        _canImprove = await UserService.HasDigitalImprovementGrantAsync(FonbecClaim.UserId);
        await LoadQueuesAsync();

        Loading = false;
    }

    private async Task ReviewSelectedAsync(long documentId)
    {
        _takingDocumentId = documentId;

        var result = await DocumentService.TakeSelectedForReviewAsync(documentId, FonbecClaim.UserId, UserRole);
        switch (result.Outcome)
        {
            case TakeSelectedReviewOutcome.Locked:
                NavigationManager.NavigateTo(NavRoutes.ReviewDocument(documentId));
                return;
            case TakeSelectedReviewOutcome.AlreadyReviewingAnother when result.DocumentId is { } heldId:
                Snackbar.Add("Este es el documento que estabas revisando.", Severity.Info);
                NavigationManager.NavigateTo(NavRoutes.ReviewDocument(heldId));
                return;
            default:
                _takingDocumentId = null;
                Snackbar.Add("Ese documento ya no está disponible.", Severity.Warning);
                await LoadQueuesAsync();
                return;
        }
    }

    private async Task LoadQueuesAsync()
    {
        _reviewQueue = await DocumentService.ListManagerReviewQueueAsync(FonbecClaim.UserId, UserRole);
        _improvementQueue = _canImprove
            ? await DocumentService.ListManagerImprovementQueueAsync(FonbecClaim.UserId, UserRole)
            : [];
    }

    private static string DocumentTypeLabel(DocumentType documentType) => documentType switch
    {
        DocumentType.Letter => "Carta",
        DocumentType.ReportCard => "Boletín",
        DocumentType.Other => "Otro documento",
        _ => string.Empty,
    };

    private static string FormatEnqueued(DateTime enqueuedAt) =>
        enqueuedAt.ToLocalTime().ToLocalizedDateTime();
}