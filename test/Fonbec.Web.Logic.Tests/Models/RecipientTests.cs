using FluentAssertions;
using FluentAssertions.Execution;
using Fonbec.Web.Logic.Models;

namespace Fonbec.Web.Logic.Tests.Models;

public class RecipientTests
{
    [Fact]
    public void ShouldUseEmailAddressForDisplayName_WhenCtorCalledWithEmailOnly()
    {
        var sut = new Recipient("john@doe.com");

        using (new AssertionScope())
        {
            sut.DisplayName.Should().Be("john@doe.com");
            sut.EmailAddress.Should().Be("<john@doe.com>");
        }
    }

    [Fact]
    public void ShouldNotUseEmailAddressForDisplayName_WhenCtorCalledWithEmailAndDisplayName()
    {
        var sut = new Recipient("john@doe.com", "John Doe");

        using (new AssertionScope())
        {
            sut.DisplayName.Should().Be("\"John Doe\"");
            sut.EmailAddress.Should().Be("<john@doe.com>");
        }
    }
}