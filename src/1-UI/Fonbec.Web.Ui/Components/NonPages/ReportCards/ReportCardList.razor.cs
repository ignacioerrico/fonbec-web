using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.Models.Students;
using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.NonPages.ReportCards;

public partial class ReportCardList
{
    [Parameter]
    [EditorRequired]
    public IReadOnlyList<ReportCardChipViewModel> Reports { get; set; } = [];

    [Parameter]
    [EditorRequired]
    public EducationLevel EducationLevel { get; set; }

    private bool IsUniversity =>
    EducationLevel == EducationLevel.University;

    private string ReportCardTitle =>
        IsUniversity ? "Libreta" : "Boletín";

    private string EmptyLabel =>
    IsUniversity ? "Sin libretas" : "Sin boletines";

    private static string ReportStatusText(ReportCardChipViewModel report) =>
    report.Status == DocumentStatus.Rejected && !string.IsNullOrWhiteSpace(report.RejectionReason)
        ? $"Rechazada: {report.RejectionReason}"
        : report.Status.Label();
}