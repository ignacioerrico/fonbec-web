using FluentAssertions;
using Fonbec.Web.Ui.Account;
using Google.Apis.Auth.AspNetCore3;
using Microsoft.AspNetCore.Authentication.Facebook;

namespace Fonbec.Web.Ui.Tests.Account;

public class ExternalSignInFailureTests
{
    [Theory]
    [InlineData("access_denied", null)]
    [InlineData(null, "access_denied;Description=The user denied the request")]
    [InlineData(null, "Access was denied by the resource owner or by the remote server.")]
    public void ToRemoteErrorCode_TreatsDenialAsCancellation(string? queryError, string? failureMessage)
    {
        ExternalSignInFailure.ToRemoteErrorCode(queryError, failureMessage).Should().Be("access_denied");
    }

    [Fact]
    public void ToRemoteErrorCode_HidesOtherFailures()
    {
        ExternalSignInFailure.ToRemoteErrorCode(null, "Correlation failed.").Should().Be("error");
    }

    [Theory]
    [InlineData(null, "/Account/ExternalLogin?RemoteError=error")]
    [InlineData("/Account/ExternalLogin?ReturnUrl=%2F&Action=LoginCallback", "/Account/ExternalLogin?RemoteError=access_denied")]
    [InlineData("/Account/Manage/ExternalLogins?Action=LinkLoginCallback", "/Account/Manage/ExternalLogins?RemoteError=error")]
    public void RedirectPath_ReturnsToLoginOrAccountLinking(string? redirectUri, string expected)
    {
        var code = expected.Contains("access_denied", StringComparison.Ordinal) ? "access_denied" : "error";
        ExternalSignInFailure.RedirectPath(redirectUri, code).Should().Be(expected);
    }

    [Fact]
    public void RedirectPath_NamesTheProviderThatFailed()
    {
        ExternalSignInFailure.RedirectPath(
                "/Account/ExternalLogin?ReturnUrl=%2F&Action=LoginCallback",
                "access_denied",
                FacebookDefaults.AuthenticationScheme)
            .Should().Be("/Account/ExternalLogin?RemoteError=access_denied&Provider=Facebook");

        ExternalSignInFailure.RedirectPath(
                "/Account/Manage/ExternalLogins?Action=LinkLoginCallback",
                "error",
                GoogleOpenIdConnectDefaults.AuthenticationScheme)
            .Should().Be($"/Account/Manage/ExternalLogins?RemoteError=error&Provider={GoogleOpenIdConnectDefaults.AuthenticationScheme}");
    }
}