using FluentAssertions;
using Fonbec.Web.Ui.Account;
using Google.Apis.Auth.AspNetCore3;

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
    public void LinkButtonText_IsSpanishForGoogle()
    {
        ExternalLoginCopy.LinkButtonText(GoogleOpenIdConnectDefaults.AuthenticationScheme, "Google")
            .Should().Be("Vincular Google");
    }

    [Theory]
    [InlineData("access_denied")]
    [InlineData("prefix-access_denied")]
    public void RemoteErrorMessage_ExplainsCancellation(string remoteError)
    {
        ExternalLoginCopy.RemoteErrorMessage(remoteError)
            .Should().Be(ExternalLoginCopy.Cancelled);
    }

    [Fact]
    public void RemoteErrorMessage_HidesTechnicalFailures()
    {
        ExternalLoginCopy.RemoteErrorMessage("correlation failed")
            .Should().Be(ExternalLoginCopy.ProviderFailed);
        ExternalLoginCopy.RemoteErrorMessage("correlation failed")
            .Should().NotContain("correlation");
    }
}