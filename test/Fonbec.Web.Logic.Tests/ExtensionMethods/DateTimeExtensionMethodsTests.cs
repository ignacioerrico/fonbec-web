using System.Globalization;
using FluentAssertions;
using Fonbec.Web.Logic.ExtensionMethods;

namespace Fonbec.Web.Logic.Tests.ExtensionMethods;

public class DateTimeExtensionMethodsTests
{
    private static readonly CultureInfo EsAr = CultureInfo.GetCultureInfo("es-AR");

    [Fact]
    public void ToSpanishMonthYear_Formats_Month_And_Year()
    {
        var date = new DateTime(2026, 6, 1);

        var result = date.ToSpanishMonthYear();

        result.Should().Be("junio de 2026");
    }

    [Theory]
    [InlineData(2026, 9, 6)]
    [InlineData(2026, 1, 31)]
    [InlineData(2026, 12, 1)]
    public void ToSpanishShortDate_Formats_Day_Month_And_Year(int year, int month, int day)
    {
        var date = new DateTime(year, month, day);

        var result = date.ToSpanishShortDate();

        result.Should().Be(date.ToString("d-MMM-yyyy", EsAr));
        result.Should().NotContain("/");
    }
}
