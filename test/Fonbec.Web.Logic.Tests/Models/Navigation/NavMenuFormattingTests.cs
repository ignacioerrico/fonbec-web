using FluentAssertions;
using Fonbec.Web.Logic.Models.Navigation;

namespace Fonbec.Web.Logic.Tests.Models.Navigation;

public class NavMenuFormattingTests
{
    [Fact]
    public void PercentNote_Keeps_The_Sign_Next_To_The_Number()
    {
        NavMenuFormatting.PercentNote(37).Should().Be("(37%)");
    }

    [Theory]
    [InlineData(0, "(0)")]
    [InlineData(24, "(24)")]
    public void CountNote_Wraps_The_Number(int count, string expected)
    {
        NavMenuFormatting.CountNote(count).Should().Be(expected);
    }

    [Theory]
    [InlineData(1, "1 bandera roja")]
    [InlineData(2, "2 banderas rojas")]
    public void RedFlags_Uses_Spanish_Plural(int count, string expected)
    {
        NavMenuFormatting.RedFlags(count).Should().Be(expected);
    }

    [Theory]
    [InlineData(1, "1 bandera verde")]
    [InlineData(4, "4 banderas verdes")]
    public void GreenFlags_Uses_Spanish_Plural(int count, string expected)
    {
        NavMenuFormatting.GreenFlags(count).Should().Be(expected);
    }

    [Theory]
    [InlineData(1, "1 pendiente")]
    [InlineData(3, "3 pendientes")]
    [InlineData(11, "Más de 10 pendientes")]
    public void PendingChanges_Describes_The_Capped_Badge(int count, string expected)
    {
        NavMenuFormatting.PendingChanges(count).Should().Be(expected);
    }
}