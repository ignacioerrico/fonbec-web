namespace Fonbec.Web.Logic.Models.Documents;

public class UploadBlobResult
{
    public required string BlobName { get; init; }

    public required string MimeType { get; init; }

    public long FileSizeBytes { get; init; }

    public required byte[] Sha256 { get; init; }
}

public class DownloadBlobResult
{
    public required Stream Content { get; init; }

    public required string MimeType { get; init; }

    public long? FileSizeBytes { get; init; }

    public byte[]? Sha256 { get; init; }
}