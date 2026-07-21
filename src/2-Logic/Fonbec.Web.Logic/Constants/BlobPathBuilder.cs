using System.Globalization;

namespace Fonbec.Web.Logic.Constants;

/// <summary>
/// Builds blob names (relative storage paths, not full URIs) following the conventions defined in US 100.
/// <para>
/// All document types share a uniform <c>chapter-{id}/student-{id}/{type}/…/{version}/{file}</c> shape so a
/// student's documents live under a single prefix and every id segment has one unambiguous meaning. Letters
/// additionally carry the recipient and plan (<c>letter/sponsor-{id}/plan-{id}</c> or
/// <c>letter/company-{id}/plan-{id}</c>). The <c>version</c> is <c>original</c> or <c>improved</c>.
/// </para>
/// <para>
/// The file name is <c>yyyy-MM-dd-{guid}.ext</c> for a single-page document, or
/// <c>yyyy-MM-dd-{n}-{guid}.ext</c> for a multi-page document, where <c>n</c> is the 1-based page order,
/// zero-padded to the width of the page count. The date is the UTC upload date. The page number, date, and
/// padding are human browsing aids only; <c>DocumentPage.PageNumber</c> remains the authoritative order.
/// </para>
/// </summary>
public static class BlobPathBuilder
{
    public static string LetterForPersonSponsor(
        int chapterId, int studentId, int sponsorId, int planId,
        string extension, bool isImproved, DateOnly uploadedOnUtc, int pageNumber, int pageCount) =>
        $"{StudentPrefix(chapterId, studentId)}/letter/sponsor-{sponsorId}/plan-{planId}/{Version(isImproved)}/{FileName(uploadedOnUtc, pageNumber, pageCount, extension)}";

    public static string LetterForCompanySponsor(
        int chapterId, int studentId, int companyId, int planId,
        string extension, bool isImproved, DateOnly uploadedOnUtc, int pageNumber, int pageCount) =>
        $"{StudentPrefix(chapterId, studentId)}/letter/company-{companyId}/plan-{planId}/{Version(isImproved)}/{FileName(uploadedOnUtc, pageNumber, pageCount, extension)}";

    public static string ReportCard(
        int chapterId, int studentId,
        string extension, bool isImproved, DateOnly uploadedOnUtc, int pageNumber, int pageCount) =>
        $"{StudentPrefix(chapterId, studentId)}/report-card/{Version(isImproved)}/{FileName(uploadedOnUtc, pageNumber, pageCount, extension)}";

    public static string Other(
        int chapterId, int studentId,
        string extension, bool isImproved, DateOnly uploadedOnUtc, int pageNumber, int pageCount) =>
        $"{StudentPrefix(chapterId, studentId)}/other/{Version(isImproved)}/{FileName(uploadedOnUtc, pageNumber, pageCount, extension)}";

    private static string StudentPrefix(int chapterId, int studentId) =>
        $"chapter-{chapterId}/student-{studentId}";

    private static string Version(bool improved) => improved ? "improved" : "original";

    private static string FileName(DateOnly uploadedOnUtc, int pageNumber, int pageCount, string extension)
    {
        var date = uploadedOnUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var guid = Guid.NewGuid().ToString("D");

        // A single-page document (the common case) carries no page number.
        if (pageCount <= 1)
        {
            return $"{date}-{guid}.{extension}";
        }

        var width = pageCount.ToString(CultureInfo.InvariantCulture).Length;
        var page = pageNumber.ToString(CultureInfo.InvariantCulture).PadLeft(width, '0');
        return $"{date}-{page}-{guid}.{extension}";
    }
}
