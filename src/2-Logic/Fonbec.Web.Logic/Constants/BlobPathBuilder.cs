namespace Fonbec.Web.Logic.Constants;

/// <summary>
/// Builds blob names (relative storage paths, not full URIs) following the
/// conventions defined in US 100. The GUID is lowercase with no braces.
/// </summary>
public static class BlobPathBuilder
{
    public static string Letter(int chapterId, int planId, int studentId, int sponsorId, string extension, bool improved) =>
        $"{chapterId}/{planId}/{studentId}/{sponsorId}/{Segment(improved)}/{NewGuid()}.{extension}";

    public static string ReportCard(int chapterId, int studentId, string extension, bool improved) =>
        $"{chapterId}/{studentId}/report-card/{Segment(improved)}/{NewGuid()}.{extension}";

    public static string Other(int chapterId, int studentId, string extension, bool improved) =>
        $"{chapterId}/{studentId}/other/{Segment(improved)}/{NewGuid()}.{extension}";

    private static string Segment(bool improved) => improved ? "improved" : "original";

    private static string NewGuid() => Guid.NewGuid().ToString("D");
}