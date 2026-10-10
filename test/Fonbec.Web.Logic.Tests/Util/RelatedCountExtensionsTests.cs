using FluentAssertions;
using Fonbec.Web.Logic.Util;

namespace Fonbec.Web.Logic.Tests.Util;

public class RelatedCountExtensionsTests
{
    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(2, false)]
    public void None_Matches_Only_Zero(int count, bool expected)
    {
        RelatedCount.None.Matches(count).Should().Be(expected);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    public void One_Matches_Only_A_Single_Relationship(int count, bool expected)
    {
        RelatedCount.One.Matches(count).Should().Be(expected);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(5, true)]
    public void TwoOrMore_Matches_At_Least_Two(int count, bool expected)
    {
        RelatedCount.TwoOrMore.Matches(count).Should().Be(expected);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(4)]
    public void Any_Matches_Every_Count(int count)
    {
        RelatedCount.Any.Matches(count).Should().BeTrue();
    }
}