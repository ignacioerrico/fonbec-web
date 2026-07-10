namespace Fonbec.Web.Logic.Constants;

/// <summary>
/// Supported document MIME types and their canonical file extensions.
/// </summary>
public static class DocumentMimeTypes
{
    public const string TextPlain = "text/plain";
    public const string Pdf = "application/pdf";
    public const string Jpeg = "image/jpeg";
    public const string Png = "image/png";

    private static readonly Dictionary<string, string> ExtensionByMimeType =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [TextPlain] = "txt",
            [Pdf] = "pdf",
            [Jpeg] = "jpg",
            [Png] = "png",
        };

    /// <summary>
    /// Image MIME types that require digital improvement before review.
    /// </summary>
    public static readonly string[] ImageMimeTypes = [Jpeg, Png];

    public static string? GetExtension(string mimeType) =>
        ExtensionByMimeType.GetValueOrDefault(mimeType);

    public static bool IsImage(string mimeType) =>
        ImageMimeTypes.Contains(mimeType, StringComparer.OrdinalIgnoreCase);
}