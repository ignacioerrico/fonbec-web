using FluentAssertions;
using Fonbec.Web.Ui.Account;
using Google.Apis.Auth.AspNetCore3;
using Microsoft.AspNetCore.Authentication.Facebook;

namespace Fonbec.Web.Ui.Tests.Account;

public class ExternalLoginCopyTests
{
    [Fact]
    public void SignInButtonText_IsSpanishForGoogle()
    {
        ExternalLoginCopy.SignInButtonText(GoogleOpenIdConnectDefaults.AuthenticationScheme, "Google OpenIdConnect")
            .Should().Be("Iniciar sesión con Google");
    }

    [Fact]
    public void SignInButtonText_IsSpanishForFacebook()
    {
        ExternalLoginCopy.SignInButtonText(FacebookDefaults.AuthenticationScheme, "Facebook")
            .Should().Be("Iniciar sesión con Facebook");
    }

    [Fact]
    public void LinkButtonText_IsSpanishForGoogle()
    {
        ExternalLoginCopy.LinkButtonText(GoogleOpenIdConnectDefaults.AuthenticationScheme, "Google")
            .Should().Be("Vincular Google");
    }

    [Fact]
    public void LinkButtonText_IsSpanishForFacebook()
    {
        ExternalLoginCopy.LinkButtonText(FacebookDefaults.AuthenticationScheme, "Facebook")
            .Should().Be("Vincular Facebook");
    }

    [Theory]
    [InlineData("access_denied")]
    [InlineData("prefix-access_denied")]
    public void RemoteErrorMessage_ExplainsGoogleCancellation(string remoteError)
    {
        ExternalLoginCopy.RemoteErrorMessage(remoteError, GoogleOpenIdConnectDefaults.AuthenticationScheme)
            .Should().Be(ExternalLoginCopy.Cancelled);
    }

    [Theory]
    [InlineData("access_denied")]
    [InlineData("prefix-access_denied")]
    public void RemoteErrorMessage_ExplainsFacebookCancellation(string remoteError)
    {
        var message = ExternalLoginCopy.RemoteErrorMessage(remoteError, FacebookDefaults.AuthenticationScheme);

        message.Should().Be(ExternalLoginCopy.FacebookCancelled);
        message.Should().Contain("Facebook");
        message.Should().NotContain("Google");
    }

    [Fact]
    public void RemoteErrorMessage_HidesTechnicalFailures()
    {
        ExternalLoginCopy.RemoteErrorMessage("correlation failed", GoogleOpenIdConnectDefaults.AuthenticationScheme)
            .Should().Be(ExternalLoginCopy.ProviderFailed);
        ExternalLoginCopy.RemoteErrorMessage("correlation failed", GoogleOpenIdConnectDefaults.AuthenticationScheme)
            .Should().NotContain("correlation");
        ExternalLoginCopy.RemoteErrorMessage("correlation failed", FacebookDefaults.AuthenticationScheme)
            .Should().Be(ExternalLoginCopy.FacebookProviderFailed);
        ExternalLoginCopy.RemoteErrorMessage("correlation failed", FacebookDefaults.AuthenticationScheme)
            .Should().NotContain("correlation");
    }

    [Fact]
    public void MissingEmailMessage_TellsTheUserToCheckFacebookPermissions()
    {
        var message = ExternalLoginCopy.MissingEmailMessage(FacebookDefaults.AuthenticationScheme);

        message.Should().Be(ExternalLoginCopy.FacebookMissingEmail);
        message.Should().Contain("contraseña");
        message.Should().NotContain("Claim");
    }
}