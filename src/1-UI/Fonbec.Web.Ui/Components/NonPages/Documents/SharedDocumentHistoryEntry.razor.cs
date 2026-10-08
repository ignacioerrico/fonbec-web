using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models.Documents;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.NonPages.Documents;

public partial class SharedDocumentHistoryEntry
{
    [Parameter, EditorRequired]
    public SharedDocumentViewModel Document { get; set; } = null!;

    [Parameter]
    public bool Expanded { get; set; }

    [Parameter]
    public bool ShowNewChip { get; set; }

    [Parameter]
    public string? RowClass { get; set; }

    [Parameter, EditorRequired]
    public Func<int, string> PageUrl { get; set; } = null!;

    [Parameter, EditorRequired]
    public EventCallback OnToggle { get; set; }

    private static bool IsDirectDownload(SharedDocumentViewModel document) =>
        document.FileKind == FileKind.Blob && document.PageCount <= 1;

    private static string ExpandIcon(SharedDocumentViewModel document, bool expanded) =>
        document.FileKind switch
        {
            FileKind.YouTube when !expanded => Icons.Material.Filled.PlayArrow,
            _ when expanded => Icons.Material.Filled.ExpandLess,
            _ => Icons.Material.Filled.ExpandMore,
        };

    private static string TypeLabel(DocumentType documentType) =>
        documentType switch
        {
            DocumentType.Letter => "Carta",
            DocumentType.ReportCard => "Boletín",
            _ => "Otro",
        };

    private static string FormatSharedOn(DateTime sharedOn) =>
        sharedOn.ToLocalTime().ToSpanishShortDate();
}