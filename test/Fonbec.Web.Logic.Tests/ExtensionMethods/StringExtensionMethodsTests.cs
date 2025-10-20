using FluentAssertions;
using Fonbec.Web.Logic.ExtensionMethods;

namespace Fonbec.Web.Logic.Tests.ExtensionMethods;

public class StringExtensionMethodsTests
{
    [Theory]
    [InlineData("María Elena", "ría", true)]
    [InlineData("María Elena", "ria", true)]
    [InlineData("Marí aelena", "ria", true)]
    [InlineData("MAR Í äelena", "ria", true)]
    [InlineData("Mar/Í/À\\elena", "ria", true)]
    public void ContainsIgnoringAccents_Success(string source, string subString, bool expected)
    {
        // Act
        var actual = source.ContainsIgnoringAccents(subString);

        // Assert
        expected.Should().Be(actual);
    }

    [Theory]
    [InlineData("María Elena")]
    [InlineData("  María   Elena  ")]
    [InlineData("MARÍA  ELENA")]
    [InlineData("maría  elena")]
    [InlineData("MarÍA  eLenA")]
    public void NormalizeText_Success(string source)
    {
        // Arrange
        const string expected = "María Elena";

        // Act
        var actual = source.NormalizeText();

        // Assert
        actual.Should().Be(expected);
    }
}