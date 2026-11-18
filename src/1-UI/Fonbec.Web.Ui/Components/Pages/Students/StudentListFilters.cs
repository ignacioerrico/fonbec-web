using Fonbec.Web.Logic.Util;

namespace Fonbec.Web.Ui.Components.Pages.Students;

internal sealed record StudentListFilters(
    string? Search,
    RelatedCount SponsorCount,
    string[]? Facilitators,
    string[]? EducationLevels,
    string[]? ReportCards,
    string[]? Chapters,
    StudentColumnFilter[]? Columns)
{
    public bool IsEmpty => string.IsNullOrWhiteSpace(Search)
        && SponsorCount == RelatedCount.Any
        && Facilitators is null
        && EducationLevels is null
        && ReportCards is null
        && Chapters is null
        && Columns is not { Length: > 0 };
}
