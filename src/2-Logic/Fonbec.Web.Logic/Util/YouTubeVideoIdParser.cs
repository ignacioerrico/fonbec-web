using System.Text.RegularExpressions;

namespace Fonbec.Web.Logic.Util;

/// <summary>
/// Extracts the 11-character YouTube video id from a full URL or accepts a bare id.
/// Supported inputs: watch URLs (<c>youtube.com/watch?v=ID</c>), short URLs
/// (<c>youtu.be/ID</c>), embed URLs (<c>youtube.com/embed/ID</c>), shorts
/// (<c>youtube.com/shorts/ID</c>), and a raw id.
/// </summary>
public static partial class YouTubeVideoIdParser
{
    private const int VideoIdLength = 11;

    public static bool TryParse(string? urlOrId, out string videoId)
    {
        videoId = string.Empty;

        if (string.IsNullOrWhiteSpace(urlOrId))
        {
            return false;
        }

        var input = urlOrId.Trim();

        // A bare video id (no scheme/host, exactly 11 valid characters).
        if (BareIdRegex().IsMatch(input))
        {
            videoId = input;
            return true;
        }

        var match = UrlRegex().Match(input);
        if (match.Success)
        {
            videoId = match.Groups["id"].Value;
            return videoId.Length == VideoIdLength;
        }

        return false;
    }

    [GeneratedRegex(@"^[A-Za-z0-9_-]{11}$", RegexOptions.CultureInvariant)]
    private static partial Regex BareIdRegex();

    [GeneratedRegex(
        @"(?:youtube\.com/(?:watch\?(?:.*&)?v=|embed/|shorts/|v/)|youtu\.be/)(?<id>[A-Za-z0-9_-]{11})",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex UrlRegex();
}