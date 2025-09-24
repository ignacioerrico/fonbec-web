using FluentAssertions;
using Fonbec.Web.Logic.Models.Results;

namespace Fonbec.Web.Logic.Tests.Models.Results;

public class CrudResultTests
{
    [Fact]
    public void AnyAffectedRows_True_When_AffectedRows_Greater_Than_Zero()
    {
        var result = new CrudResult(3);
        result.AnyAffectedRows.Should().BeTrue();
    }

    [Fact]
    public void AnyAffectedRows_False_When_AffectedRows_Zero()
    {
        var result = new CrudResult(0);
        result.AnyAffectedRows.Should().BeFalse();
    }

    [Fact]
    public void AffectedRows_Defaults_To_Zero()
    {
        var result = new CrudResult();
        result.AffectedRows.Should().Be(0);
    }
}
