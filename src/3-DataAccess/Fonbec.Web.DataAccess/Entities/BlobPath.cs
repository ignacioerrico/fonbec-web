namespace Fonbec.Web.DataAccess.Entities;

public class BlobPath
{
    public long BlobPathId { get; set; }

    public string StoragePath { get; set; } = string.Empty;

    public string MimeType { get; set; } = string.Empty;

    public long? FileSizeBytes { get; set; }

    public byte[]? Sha256 { get; set; }
}