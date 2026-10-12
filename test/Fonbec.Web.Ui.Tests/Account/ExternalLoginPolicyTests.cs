using System.Security.Claims;
using FluentAssertions;
using Fonbec.Web.Ui.Account;

namespace Fonbec.Web.Ui.Tests.Account;

public class ExternalLoginPolicyTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void NormalizeEmail_RejectsBlank(string? email)
    {
        ExternalLoginPolicy.NormalizeEmail(email).Should().BeNull();
    }

    [Fact]
    public void NormalizeEmail_TrimsAndLowercases()
    {
        ExternalLoginPolicy.NormalizeEmail("  User@Example.COM ").Should().Be("user@example.com");
    }

    [Fact]
    public void ReadEmail_AcceptsTheIdentityEmailClaim()
    {
        var principal = Principal(new Claim(ClaimTypes.Email, "Ada@Fonbec.org"));

        ExternalLoginPolicy.ReadEmail(principal).Should().Be("ada@fonbec.org");
    }

    [Fact]
    public void ReadEmail_AcceptsTheOpenIdEmailClaim()
    {
        var principal = Principal(new Claim("email", "Ada@Fonbec.org"));

        ExternalLoginPolicy.ReadEmail(principal).Should().Be("ada@fonbec.org");
    }

    [Fact]
    public void EmailsMatch_IgnoresCaseAndSurroundingSpaces()
    {
        ExternalLoginPolicy.EmailsMatch(" Ada@Fonbec.org ", "ada@fonbec.org").Should().BeTrue();
    }

    [Fact]
    public void EmailsMatch_RejectsADifferentGoogleAccount()
    {
        ExternalLoginPolicy.EmailsMatch("ada@fonbec.org", "other@gmail.com").Should().BeFalse();
        ExternalLoginPolicy.EmailsMatch("ada@fonbec.org", null).Should().BeFalse();
        ExternalLoginPolicy.EmailsMatch(null, "ada@fonbec.org").Should().BeFalse();
    }

    [Fact]
    public void Decide_SignsInWhenTheProviderIsAlreadyLinked()
    {
        ExternalLoginPolicy.Decide(
                alreadySignedIn: true,
                alreadyLinkedAndLockedOut: false,
                alreadyLinkedAndNotAllowed: false,
                matchingUserExists: true,
                matchingUserLockedOut: false)
            .Should().Be(ExternalLoginDecision.SignIn);
    }

    [Fact]
    public void Decide_LinksAnExistingUserWithTheSameEmail()
    {
        ExternalLoginPolicy.Decide(
                alreadySignedIn: false,
                alreadyLinkedAndLockedOut: false,
                alreadyLinkedAndNotAllowed: false,
                matchingUserExists: true,
                matchingUserLockedOut: false)
            .Should().Be(ExternalLoginDecision.LinkAndSignIn);
    }

    [Fact]
    public void Decide_DoesNotProvisionAnUnknownEmail()
    {
        ExternalLoginPolicy.Decide(
                alreadySignedIn: false,
                alreadyLinkedAndLockedOut: false,
                alreadyLinkedAndNotAllowed: false,
                matchingUserExists: false,
                matchingUserLockedOut: false)
            .Should().Be(ExternalLoginDecision.NotProvisioned);
    }

    [Fact]
    public void Decide_LocksOutBeforeLinkingADisabledUser()
    {
        ExternalLoginPolicy.Decide(
                alreadySignedIn: false,
                alreadyLinkedAndLockedOut: false,
                alreadyLinkedAndNotAllowed: false,
                matchingUserExists: true,
                matchingUserLockedOut: true)
            .Should().Be(ExternalLoginDecision.Lockout);
    }

    [Fact]
    public void Decide_LocksOutAnAlreadyLinkedDisabledUser()
    {
        ExternalLoginPolicy.Decide(
                alreadySignedIn: false,
                alreadyLinkedAndLockedOut: true,
                alreadyLinkedAndNotAllowed: false,
                matchingUserExists: false,
                matchingUserLockedOut: false)
            .Should().Be(ExternalLoginDecision.Lockout);
    }

    [Fact]
    public void Decisions_DoNotIncludeCreatingAUser()
    {
        Enum.GetNames<ExternalLoginDecision>()
            .Should().NotContain(name => name.Contains("Create", StringComparison.OrdinalIgnoreCase));
    }

    private static ClaimsPrincipal Principal(Claim claim) =>
        new(new ClaimsIdentity([claim]));
}