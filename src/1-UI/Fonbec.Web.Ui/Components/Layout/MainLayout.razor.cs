using System.Security.Claims;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Account;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Layout;

public partial class MainLayout
{
    private bool _isDarkMode;
    private bool _loadedFromAccount;
    private int _themeVersion;

    [Inject]
    public IUserService UserService { get; set; } = null!;

    [Inject]
    public IJSRuntime JsRuntime { get; set; } = null!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = null!;

    [Inject]
    public IHttpContextAccessor HttpContextAccessor { get; set; } = null!;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationState { get; set; }

    [CascadingParameter]
    private HttpContext? HttpContext { get; set; }

    private HttpContext? RequestHttpContext => HttpContext ?? HttpContextAccessor.HttpContext;

    private bool IsStaticRender => RendererInfo.Name == "Static";

    private string ThemeTooltip => _isDarkMode ? "Tema claro" : "Tema oscuro";

    private string ThemeIcon => _isDarkMode
        ? Icons.Material.Filled.LightMode
        : Icons.Material.Filled.DarkMode;

    private string NextIsDarkValue => _isDarkMode ? "false" : "true";

    private string ReturnUrl => NavigationManager.ToBaseRelativePath(NavigationManager.Uri);

    protected override async Task OnInitializedAsync()
    {
        var userId = await CurrentUserIdAsync();
        if (userId is null)
        {
            _isDarkMode = RequestHttpContext is not null && ThemeCookie.IsDark(RequestHttpContext.Request);
            return;
        }

        _isDarkMode = await UserService.GetIsDarkModeAsync(userId.Value);
        _loadedFromAccount = true;
        TrySetThemeCookie(_isDarkMode);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || !_loadedFromAccount || IsStaticRender)
        {
            return;
        }

        await WriteThemeCookieAsync(_isDarkMode);
    }

    private async Task ToggleThemeAsync()
    {
        var userId = await CurrentUserIdAsync();
        if (userId is null)
        {
            return;
        }

        var next = !_isDarkMode;
        _isDarkMode = next;

        try
        {
            await UserService.SetIsDarkModeAsync(userId.Value, next);
        }
        catch
        {
            _isDarkMode = !next;
            throw;
        }

        await WriteThemeCookieAsync(next);
    }

    private async Task WriteThemeCookieAsync(bool isDark)
    {
        var version = ++_themeVersion;
        try
        {
            await JsRuntime.InvokeVoidAsync("fonbecTheme.set", ThemeCookie.Format(isDark));
            if (version != _themeVersion)
            {
                await JsRuntime.InvokeVoidAsync("fonbecTheme.set", ThemeCookie.Format(_isDarkMode));
            }
        }
        catch (JSDisconnectedException)
        {
        }
    }

    private async Task<int?> CurrentUserIdAsync()
    {
        if (AuthenticationState is null)
        {
            return null;
        }

        var user = (await AuthenticationState).User;
        if (user.Identity is not { IsAuthenticated: true })
        {
            return null;
        }

        var userIdValue = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdValue, out var userId) ? userId : null;
    }

    private void TrySetThemeCookie(bool isDark)
    {
        if (RequestHttpContext is not { Response.HasStarted: false })
        {
            return;
        }

        try
        {
            ThemeCookie.Set(RequestHttpContext.Response, isDark);
        }
        catch (InvalidOperationException)
        {
            // The response was already committed. The interactive toggle writes the same cookie.
        }
    }
}