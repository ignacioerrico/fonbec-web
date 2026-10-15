using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models.Documents;
using Fonbec.Web.Logic.Models.Documents.Input;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Components.Layout;
using Fonbec.Web.Ui.Constants;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Review;

[PageMetadata(nameof(RejectedDocuments), "Documentos rechazados", [FonbecRole.Manager])]
public partial class RejectedDocuments : AuthenticationRequiredComponentBase
{
    private HeldRejectionsPageViewModel? _page;
    private long? _busyDocumentId;
    private readonly HashSet<long> _openPreviews = [];

    private static readonly Dictionary<string, object> NoEnhance = new()
    {
        ["data-enhance-nav"] = "false",
    };

    [Inject]
    public IDocumentService DocumentService { get; set; } = null!;

    [Inject]
    public IDialogService DialogService { get; set; } = null!;

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
        _page = await DocumentService.GetHeldRejectionsPageAsync(chapterId, UserRole);
        Loading = false;

        if (_page is null)
        {
            Snackbar.Add(DocumentMessages.ChapterNotFound, Severity.Error);
            NavigationManager.NavigateTo(NavRoutes.Home);
        }
    }

    private async Task ShareAsync(HeldRejectionItemViewModel item)
    {
        var with = ShareWithPhrase(item);
        var confirmed = await DialogService.ShowMessageBox(
            "Aprobar documento",
            $"¿Aprobar el documento de {item.StudentName}? Se cancela el rechazo y se comparte con {with}.",
            yesText: "Aprobar",
            cancelText: "Cancelar");
        if (confirmed is not true || FonbecClaim.ChapterId is not int chapterId)
        {
            return;
        }

        await DecideAsync(
            item,
            () => DocumentService.ShareHeldRejectionAsync(Decision(item, chapterId)),
            $"Se aprobó el documento y se compartió con {with}.");
    }

    private async Task ReturnAsync(HeldRejectionItemViewModel item)
    {
        var confirmed = await DialogService.ShowMessageBox(
            "Rechazar documento",
            $"¿Rechazar el documento de {item.StudentName}? El mediador va a recibir un correo para subir uno nuevo.",
            yesText: "Rechazar",
            cancelText: "Cancelar");
        if (confirmed is not true || FonbecClaim.ChapterId is not int chapterId)
        {
            return;
        }

        await DecideAsync(
            item,
            () => DocumentService.ReturnHeldRejectionAsync(Decision(item, chapterId)),
            "Se rechazó el documento y se avisó al mediador.");
    }

    private async Task DecideAsync(
        HeldRejectionItemViewModel item,
        Func<Task<Fonbec.Web.Logic.Models.Results.ReviewResult>> decide,
        string successMessage)
    {
        _busyDocumentId = item.DocumentId;
        var result = await decide();
        _busyDocumentId = null;

        if (!result.IsSuccess)
        {
            Snackbar.Add(result.Errors?.FirstOrDefault() ?? DocumentMessages.DocumentSaveFailed, Severity.Error);
            await LoadAsync();
            return;
        }

        Snackbar.Add(successMessage, Severity.Success);
        NavMenuRefresh.NotifyChanged();
        await LoadAsync();
    }

    private HeldRejectionDecisionInputModel Decision(HeldRejectionItemViewModel item, int chapterId) =>
        new(item.DocumentId, FonbecClaim.UserId, UserRole, chapterId, item.RowVersion);

    private static string TypeLabel(DocumentType documentType) => documentType switch
    {
        DocumentType.Letter => "Carta",
        DocumentType.ReportCard => "Boletín",
        DocumentType.Other => "Otro documento",
        _ => documentType.ToString(),
    };

    private string SponsorColumnHeader =>
        _page is null || _page.Items.Count == 0
            ? "De padrino"
            : SingleSponsorColumnLabel(_page.Items.Select(SponsorColumnLabel));

    private static string SponsorColumnLabel(HeldRejectionItemViewModel item)
    {
        var people = item.Sponsors.Where(sponsor => !sponsor.IsCompany).ToList();
        if (people.Count > 0 && people.All(sponsor => sponsor.Gender == Gender.Female))
        {
            return "De madrina";
        }

        if (people.Any(sponsor => sponsor.Gender == Gender.Female)
            && people.Any(sponsor => sponsor.Gender != Gender.Female))
        {
            return "De padrino/madrina";
        }

        return "De padrino";
    }

    private static string SingleSponsorColumnLabel(IEnumerable<string> labels)
    {
        var distinct = labels.Distinct().ToList();
        return distinct.Count == 1 ? distinct[0] : "De padrino/madrina";
    }

    private static string SponsorNames(HeldRejectionItemViewModel item)
    {
        var names = item.Sponsors.Select(sponsor => sponsor.Name).Where(name => !string.IsNullOrWhiteSpace(name));
        var text = string.Join(", ", names);
        return string.IsNullOrWhiteSpace(text) ? "—" : text;
    }

    private static string ApproveTooltip(HeldRejectionItemViewModel item) =>
        $"Cancela el rechazo y comparte el documento con {ShareWithPhrase(item)}";

    private static string ShareWithPhrase(HeldRejectionItemViewModel item)
    {
        var sponsors = item.Sponsors;
        if (sponsors.Count == 0)
        {
            return "el padrino/la madrina";
        }

        if (sponsors.Count == 1)
        {
            return OneSponsorPhrase(sponsors[0]);
        }

        if (sponsors.All(sponsor => sponsor.IsCompany))
        {
            return "las empresas";
        }

        if (sponsors.All(sponsor => !sponsor.IsCompany && sponsor.Gender == Gender.Female))
        {
            return "las madrinas";
        }

        if (sponsors.All(sponsor => !sponsor.IsCompany && sponsor.Gender == Gender.Male))
        {
            return "los padrinos";
        }

        return "los padrinos y madrinas";
    }

    private static string OneSponsorPhrase(HeldRejectionSponsorViewModel sponsor)
    {
        if (sponsor.IsCompany)
        {
            return "la empresa";
        }

        return sponsor.Gender switch
        {
            Gender.Male => "el padrino",
            Gender.Female => "la madrina",
            _ => "el padrino/la madrina",
        };
    }

    private static string FormatRejectedOn(HeldRejectionItemViewModel item)
    {
        if (item.RejectedOn is null)
        {
            return "—";
        }

        var local = item.RejectedOn.Value.ToLocalTime();
        return $"{local.ToSpanishShortDate()} a las {local:HH:mm}";
    }

    private string ViewUrl(HeldRejectionItemViewModel item, int pageNumber) =>
        NavRoutes.ManagerStudentDocumentPage(item.StudentId, item.DocumentId, pageNumber);

    private void TogglePreview(long documentId)
    {
        if (!_openPreviews.Add(documentId))
        {
            _openPreviews.Remove(documentId);
        }
    }

    private bool IsPreviewOpen(long documentId) => _openPreviews.Contains(documentId);
}