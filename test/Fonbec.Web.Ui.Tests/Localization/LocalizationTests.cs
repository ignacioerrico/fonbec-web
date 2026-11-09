using System.Globalization;
using Fonbec.Web.Ui.Localization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using MudBlazor;

namespace Fonbec.Web.Ui.Tests.Localization;

public sealed class LocalizationTests
{
    [Fact]
    public void English_fallback_matches_english_catalog_and_spanish_placeholders()
    {
        var manager = new System.Resources.ResourceManager(
            "Fonbec.Web.Ui.Resources.Localization.SharedResources", typeof(SharedResources).Assembly);
        var neutral = manager.GetResourceSet(CultureInfo.InvariantCulture, true, false)!;
        var english = manager.GetResourceSet(new("en"), true, false)!;
        var spanish = manager.GetResourceSet(new("es"), true, false)!;
        foreach (System.Collections.DictionaryEntry entry in neutral)
        {
            var key = (string)entry.Key;
            Assert.Equal(entry.Value, english.GetString(key));
            var translated = spanish.GetString(key);
            if (translated is null) continue; // Deliberate omissions use English fallback.
            static string[] Placeholders(string text) => System.Text.RegularExpressions.Regex
                .Matches(text, @"\{[^{}]+\}").Select(match => match.Value).Order().ToArray();
            Assert.Equal(Placeholders((string)entry.Value!), Placeholders(translated));
        }
        foreach (System.Collections.DictionaryEntry entry in spanish)
            Assert.NotNull(neutral.GetString((string)entry.Key));
    }

    private static ServiceProvider Services() => new ServiceCollection()
        .AddLogging().AddFonbecLocalization().BuildServiceProvider();

    [Theory]
    [InlineData("es", "Iniciar sesión")]
    [InlineData("es-AR", "Iniciar sesión")]
    [InlineData("en", "Sign in")]
    public void Production_resources_resolve_for_each_language(string culture, string expected)
    {
        using var services = Services();
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new(culture);
            var strings = services.GetRequiredService<IStringLocalizer<SharedResources>>();
            Assert.Equal(expected, strings["SignIn"].Value);
            Assert.True(strings["UnknownKey"].ResourceNotFound);
            Assert.Equal("UnknownKey", strings["UnknownKey"].Value);
        }
        finally { CultureInfo.CurrentUICulture = previous; }
    }

    [Fact]
    public void Missing_spanish_key_falls_back_to_neutral_english()
    {
        using var services = Services();
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new("es");
            var strings = services.GetRequiredService<IStringLocalizer<LocalizationFixture>>();
            Assert.Equal("Español", strings["Translated"].Value);
            Assert.Equal("English fallback", strings["EnglishOnly"].Value);
            Assert.False(strings["EnglishOnly"].ResourceNotFound);
        }
        finally { CultureInfo.CurrentUICulture = previous; }
    }

    [Theory]
    [InlineData("", "", "es", "es-AR")]
    [InlineData("?culture=en", "", "en", "en")]
    [InlineData("?culture=es", "", "es", "es-AR")]
    [InlineData("?culture=xx", "", "es", "es-AR")]
    [InlineData("", ".AspNetCore.Culture=c=en|uic=en", "en", "en")]
    public async Task Middleware_selects_culture_for_requests_and_persists_explicit_choices(
        string query, string cookie, string ui, string formatting)
    {
        using var services = Services();
        var app = new ApplicationBuilder(services);
        app.UseFonbecLocalization();
        app.Run(context =>
        {
            Assert.Equal(ui, CultureInfo.CurrentUICulture.Name);
            Assert.Equal(formatting, CultureInfo.CurrentCulture.Name);
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.QueryString = new(query);
        context.Request.Headers.Cookie = cookie;
        context.Request.Headers.AcceptLanguage = "en-US,en;q=0.9";
        await app.Build()(context);
        if (query is "?culture=en" or "?culture=es")
            Assert.Contains(".AspNetCore.Culture=", context.Response.Headers.SetCookie.ToString());
        else
            Assert.Equal(0, context.Response.Headers.SetCookie.Count);
    }

    [Fact]
    public void Mud_grid_and_annotation_strings_are_spanish()
    {
        using var services = Services();
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new("es");
            Assert.Equal("Guardar", services.GetRequiredService<MudLocalizer>()["MudDataGrid_Save"].Value);
            Assert.Equal("Este campo es obligatorio.", ValidationResources.Required);
        }
        finally { CultureInfo.CurrentUICulture = previous; }
    }
}

public sealed class LocalizationFixture;
