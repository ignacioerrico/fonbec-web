namespace Fonbec.Web.Logic.Options;

public class BlobStorageOptions
{
    public const string SectionName = "BlobStorage";

    public string ContainerName { get; set; } = "documents";

    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;

    public string[] AllowedMimeTypes { get; set; } =
    [
        "text/plain",
        "application/pdf",
        "image/jpeg",
        "image/png",
    ];
}