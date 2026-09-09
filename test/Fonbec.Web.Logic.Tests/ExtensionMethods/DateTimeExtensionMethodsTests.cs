using FluentAssertions;
using Fonbec.Web.Logic.ExtensionMethods;

namespace Fonbec.Web.Logic.Tests.ExtensionMethods;

public class DateTimeExtensionMethodsTests
{
    [Fact]
    public void ToSpanishMonthYear_Formats_Month_And_Year()
    {
        var date = new DateTime(2026, 6, 1);

        var result = date.ToSpanishMonthYear();

        result.Should().Be("Junio de 2026");
    }
}
