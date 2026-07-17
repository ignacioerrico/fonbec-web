using FluentAssertions;
using Fonbec.Web.Logic.Util;

namespace Fonbec.Web.Logic.Tests.Util;

public class YouTubeVideoIdParserTests
{
    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://youtube.com/watch?v=dQw4w9WgXcQ&t=42s", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/watch?list=PL123&v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ?si=abc", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/shorts/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("  dQw4w9WgXcQ  ", "dQw4w9WgXcQ")]
    public void TryParse_ValidInput_ReturnsVideoId(string input, string expected)
    {
        var success = YouTubeVideoIdParser.TryParse(input, out var videoId);

        success.Should().BeTrue();
        videoId.Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a youtube link")]
    [InlineData("https://vimeo.com/12345678")]
    [InlineData("https://www.youtube.com/watch?v=short")]
    [InlineData("tooShortId")]
    public void TryParse_InvalidInput_ReturnsFalse(string? input)
    {
        var success = YouTubeVideoIdParser.TryParse(input, out var videoId);

        success.Should().BeFalse();
        videoId.Should().BeEmpty();
    }
}