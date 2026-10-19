using FluentAssertions;
using Fonbec.Web.Ui.Components.Errors;

namespace Fonbec.Web.Ui.Tests.Components.Errors;

public class ExceptionLogSanitizerTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("students", "students")]
    [InlineData("students?email=a@b.com", "students")]
    [InlineData("students?token=abc#section", "students")]
    [InlineData("https://localhost:5001/students?x=1", "https://localhost:5001/students")]
    [InlineData("students#top", "students")]
    [InlineData("?only=query", "")]
    public void StripQuery_RemovesQueryAndFragment(string? uriOrPath, string expected)
    {
        ExceptionLogSanitizer.StripQuery(uriOrPath).Should().Be(expected);
    }

    [Fact]
    public void RedactMessage_LeavesOrdinaryText()
    {
        ExceptionLogSanitizer.RedactMessage("The student list failed to load.").Should().Be("The student list failed to load.");
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    public void RedactMessage_EmptyStaysEmpty(string? message, string expected)
    {
        ExceptionLogSanitizer.RedactMessage(message).Should().Be(expected);
    }

    [Fact]
    public void RedactMessage_RedactsEmail()
    {
        ExceptionLogSanitizer.RedactMessage("failed for jane.doe@example.com")
            .Should().Be("failed for [redacted]");
    }

    [Fact]
    public void RedactMessage_RedactsBearerToken()
    {
        ExceptionLogSanitizer.RedactMessage("Authorization: Bearer abc.def-123==")
            .Should().Be("Authorization: Bearer [redacted]");
    }

    [Theory]
    [InlineData("password=hunter2", "password=[redacted]")]
    [InlineData("Token: abc.def", "Token: [redacted]")]
    [InlineData("api_key=xyz", "api_key=[redacted]")]
    [InlineData("connectionstring=Server=db;User=sa", "connectionstring=[redacted]")]
    [InlineData("The password = hunter2 was rejected", "The password = [redacted] was rejected")]
    public void RedactMessage_RedactsSecretAssignments(string message, string expected)
    {
        ExceptionLogSanitizer.RedactMessage(message).Should().Be(expected);
    }

    [Fact]
    public void RedactMessage_RedactsEmailAndPasswordTogether()
    {
        ExceptionLogSanitizer.RedactMessage("login jane@example.com password=hunter2")
            .Should().Be("login [redacted] password=[redacted]");
    }
}