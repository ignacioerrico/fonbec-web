using FluentAssertions;
using Fonbec.Web.Ui.Account;

namespace Fonbec.Web.Ui.Tests.Account;

public class GoogleSignInFailureTests
{
    [Theory]
    [InlineData("access_denied", null)]
    [InlineData(null, "access_denied;Description=The user denied the request")]
    [InlineData(null, "Access was denied by the resource owner or by the remote server.")]
    public void ToRemoteErrorCode_TreatsDenialAsCancellation(string? queryError, string? failureMessage)
    {
        GoogleSignInFailure.ToRemoteErrorCode(queryError, failureMessage).Should().Be("access_denied");
    }

    [Fact]
    public void ToRemoteErrorCode_HidesOtherFailures()
    {
        GoogleSignInFailure.ToRemoteErrorCode(null, "Correlation failed.").Should().Be("error");
    }

    [Theory]
    [InlineData(null, "/Account/ExternalLogin?RemoteError=error")]
    [InlineData("/Account/ExternalLogin?ReturnUrl=%2F&Action=LoginCallback", "/Account/ExternalLogin?RemoteError=access_denied")]
    [InlineData("/Account/Manage/ExternalLogins?Action=LinkLoginCallback", "/Account/Manage/ExternalLogins?RemoteError=error")]
    public void RedirectPath_ReturnsToLoginOrAccountLinking(string? redirectUri, string expected)
    {
        var code = expected.Contains("access_denied", StringComparison.Ordinal) ? "access_denied" : "error";
        GoogleSignInFailure.RedirectPath(redirectUri, code).Should().Be(expected);
    }
}