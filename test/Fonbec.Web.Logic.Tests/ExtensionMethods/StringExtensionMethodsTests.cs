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

    [Theory]
    [InlineData("abc")]
    [InlineData(" abc ")] // method does NOT trim; it returns original
    [InlineData(" a  b ")]
    [InlineData("Áccented")]
    public void MustBeNonEmpty_ReturnsOriginal_WhenNonEmpty(string value)
    {
        // Act
        var result = value.MustBeNonEmpty();

        // Assert
        result.Should().BeSameAs(value); // same reference (string interning may vary) but content equality is enough too
        result.Should().Be(value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\r\n\t")]
    public void MustBeNonEmpty_ThrowsArgumentException_WhenNullOrWhitespace(string? value)
    {
        // Act
        var act = () => value!.MustBeNonEmpty();

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("String must be non-empty. (Parameter 'value')")
            .And.ParamName.Should().Be("value");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n ")]
    public void NullOrTrimmed_ReturnsNull_WhenNullOrWhitespace(string? value)
    {
        // Act
        var result = value!.NullOrTrimmed();

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("abc", "abc")]
    [InlineData(" abc ", "abc")]
    [InlineData("  a b  ", "a b")] // internal spacing preserved; only trimmed
    [InlineData("\tabc\r\n", "abc")]
    [InlineData("  a  b  ", "a  b")] // multiple internal spaces preserved
    public void NullOrTrimmed_ReturnsTrimmedString_WhenNonWhitespace(string value, string expected)
    {
        // Act
        var result = value.NullOrTrimmed();

        // Assert
        result.Should().Be(expected);
    }
}