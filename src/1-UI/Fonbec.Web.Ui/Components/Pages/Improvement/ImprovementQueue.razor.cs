using Fonbec.Web.Logic.Models.Documents;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Constants;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Improvement;

public partial class ImprovementQueue : AuthenticationRequiredComponentBase
{
    private ReviewProgressViewModel _progress = new();
    private bool _takingNext;

    [Inject]
    public IDocumentService DocumentService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        Loading = true;

        var activeLockDocumentId = await DocumentService.GetActiveImprovementLockAsync(FonbecClaim.UserId, UserRole);
        if (activeLockDocumentId is { } documentId)
        {
            Snackbar.Add("Este es el documento que estabas mejorando.", Severity.Info);
            NavigationManager.NavigateTo(NavRoutes.ImproveDocument(documentId));
            return;
        }

        _progress = await DocumentService.GetGlobalReviewProgressAsync(FonbecClaim.UserId, UserRole, null);

        Loading = false;
    }

    private async Task ImproveNextAsync()
    {
        _takingNext = true;

        var next = await DocumentService.TakeNextForDigitalImprovementAsync(FonbecClaim.UserId, UserRole);

        if (next is null)
        {
            _takingNext = false;
            Snackbar.Add("No hay imágenes pendientes de mejora.", Severity.Info);
            return;
        }

        NavigationManager.NavigateTo(NavRoutes.ImproveDocument(next.DocumentId));
    }
}