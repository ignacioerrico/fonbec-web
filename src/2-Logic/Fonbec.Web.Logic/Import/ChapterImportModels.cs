namespace Fonbec.Web.Logic.Import;

public enum ChapterImportUploadKind
{
    Excel,
    StudentsCsv,
    SponsorsCsv,
}

public enum ChapterImportRowStatus
{
    Create,
    Skip,
    Error,
    Created,
    Failed,
}

public sealed record ChapterImportUpload(
    string FileName,
    byte[] Content,
    ChapterImportUploadKind Kind);

public sealed record ChapterImportRequest(
    int ChapterId,
    int? CallerChapterId,
    int CreatedById,
    int? DefaultFacilitatorId,
    IReadOnlyList<ChapterImportUpload> Files);

public sealed record ChapterImportRowResult(
    string Sheet,
    int RowNumber,
    string Name,
    ChapterImportRowStatus Status,
    string? Detail);

public sealed class ChapterImportResult
{
    public IReadOnlyList<string> FileErrors { get; init; } = [];

    public IReadOnlyList<ChapterImportRowResult> Rows { get; init; } = [];
}

public sealed class ChapterImportParsedRow
{
    public required string Sheet { get; init; }

    public required int RowNumber { get; init; }

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string NickName { get; init; } = string.Empty;

    public string Gender { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Phone { get; init; } = string.Empty;

    public string Notes { get; init; } = string.Empty;

    public string SecondaryYear { get; init; } = string.Empty;

    public string UniversityYear { get; init; } = string.Empty;

    public string FacilitatorEmail { get; init; } = string.Empty;
}

public sealed class ChapterImportParseResult
{
    public List<string> FileErrors { get; } = [];

    public List<ChapterImportParsedRow> Students { get; } = [];

    public List<ChapterImportParsedRow> Sponsors { get; } = [];
}