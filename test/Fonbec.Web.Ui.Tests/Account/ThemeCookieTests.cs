using FluentAssertions;
using Fonbec.Web.Ui.Account;
using Microsoft.AspNetCore.Http;

namespace Fonbec.Web.Ui.Tests.Account;

public class ThemeCookieTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("light", false)]
    [InlineData("dark", true)]
    [InlineData("Dark", false)]
    [InlineData("blue", false)]
    [InlineData("", false)]
    [InlineData("true", false)]
    public void IsDark_AcceptsOnlyDark(string? value, bool expected)
    {
        var context = new DefaultHttpContext();
        if (value is not null)
        {
            context.Request.Headers.Cookie = $"{ThemeCookie.Name}={value}";
        }

        ThemeCookie.IsDark(context.Request).Should().Be(expected);
    }

    [Fact]
    public void Set_WritesDarkForOneYear_OnHttps()
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";

        ThemeCookie.Set(context.Response, true);

        var header = context.Response.Headers.SetCookie.ToString();
        header.Should().Contain($"{ThemeCookie.Name}={ThemeCookie.Dark}");
        header.Should().Contain("path=/");
        header.Should().Contain("samesite=lax");
        header.Should().Contain("secure");
        header.Should().NotContain("httponly");
        ExpiresAboutOneYearFromNow(header).Should().BeTrue();
    }

    [Fact]
    public void Set_WritesLight_WithoutSecure_OnHttp()
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "http";

        ThemeCookie.Set(context.Response, false);

        var header = context.Response.Headers.SetCookie.ToString();
        header.Should().Contain($"{ThemeCookie.Name}={ThemeCookie.Light}");
        header.Should().NotContain("secure");
        header.Should().NotContain("httponly");
    }

    [Theory]
    [InlineData(null, "~/")]
    [InlineData("", "~/")]
    [InlineData("   ", "~/")]
    [InlineData("Account/Login", "~/Account/Login")]
    [InlineData("/Account/Manage", "~/Account/Manage")]
    [InlineData("Account/Manage?x=1", "~/Account/Manage?x=1")]
    [InlineData("https://evil.test/Account/Login", "~/")]
    [InlineData("//evil.test", "~/")]
    [InlineData("/\\evil.test", "~/")]
    public void NormalizeReturnUrl_StaysOnThisSite(string? returnUrl, string expected)
    {
        ThemeCookie.NormalizeReturnUrl(returnUrl).Should().Be(expected);
    }

    private static bool ExpiresAboutOneYearFromNow(string setCookie)
    {
        const string marker = "expires=";
        var start = setCookie.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
        {
            return false;
        }

        start += marker.Length;
        var end = setCookie.IndexOf(';', start);
        var raw = end < 0 ? setCookie[start..] : setCookie[start..end];
        if (!DateTimeOffset.TryParse(raw, out var expires))
        {
            return false;
        }

        var remaining = expires - DateTimeOffset.UtcNow;
        return remaining > TimeSpan.FromDays(360) && remaining < TimeSpan.FromDays(370);
    }
}