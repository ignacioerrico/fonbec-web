using FluentAssertions;
using Fonbec.Web.Logic.Constants;

namespace Fonbec.Web.Logic.Tests.Constants;

public class FonbecAuthTests
{
    [Fact]
    public void ClaimType_IsFonbecAuth()
    {
        FonbecAuth.ClaimType.Should().Be("FonbecAuth");
    }
}