using FluentAssertions;
using Fonbec.Web.Logic.Models;

namespace Fonbec.Web.Logic.Tests.Models;

public class SelectableModelTests
{
    [Fact]
    public void Constructor_Sets_Properties()
    {
        var model = new SelectableModel<int>(42, "Test");
        
        model.Key.Should().Be(42);
        model.DisplayName.Should().Be("Test");
    }

    [Fact]
    public void ToString_Returns_DisplayName()
    {
        var model = new SelectableModel<int>(1, "Display");

        model.ToString().Should().Be("Display");
    }

    [Fact]
    public void Equality_Based_On_Key()
    {
        var a = new SelectableModel<int>(1, "A");
        var b = new SelectableModel<int>(1, "B");
        var c = new SelectableModel<int>(2, "A");

        (a == b).Should().BeTrue();
        (a != c).Should().BeTrue();
        a.Equals(b).Should().BeTrue();
        a.Equals(c).Should().BeFalse();
        a.Equals((object?)b).Should().BeTrue();
        a.Equals((object?)c).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_Based_On_Key()
    {
        var a = new SelectableModel<int>(5, "A");
        var b = new SelectableModel<int>(5, "B");

        a.GetHashCode().Should().Be(b.GetHashCode());
    }
}
