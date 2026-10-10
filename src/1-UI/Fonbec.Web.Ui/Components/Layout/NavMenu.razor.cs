using System.Security.Claims;
using Fonbec.Web.Logic.Constants;
using Fonbec.Web.Logic.Models.Navigation;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Constants;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;

namespace Fonbec.Web.Ui.Components.Layout;

public sealed partial class NavMenu : ComponentBase, IDisposable
{
    private NavMenuIndicators _indicators = NavMenuIndicators.Empty;
    private int _reloadVersion;
    private bool _disposed;

    // The Account section renders statically, so leaving it means crossing into interactive
    // rendering. Patching the DOM across that boundary flashes; a full load does not.
    private string? EnhancedNav => RendererInfo.Name == "Static" ? "false" : null;

    [Inject]
    public INavMenuService NavMenuService { get; set; } = null!;

    [Inject]
    public IUserService UserService { get; set; } = null!;

    [Inject]
    public NavMenuRefresh NavMenuRefresh { get; set; } = null!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = null!;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationState { get; set; }

    protected override async Task OnInitializedAsync()
    {
        NavigationManager.LocationChanged += OnLocationChanged;
        NavMenuRefresh.Changed += OnMenuRefresh;
        await ReloadAsync();
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e) =>
        _ = InvokeAsync(ReloadAsync);

    private void OnMenuRefresh() => _ = InvokeAsync(ReloadAsync);

    private async Task ReloadAsync()
    {
        var version = Interlocked.Increment(ref _reloadVersion);

        if (AuthenticationState is null)
        {
            return;
        }

        var user = (await AuthenticationState).User;
        if (_disposed || version != _reloadVersion)
        {
            return;
        }

        if (user.Identity is not { IsAuthenticated: true })
        {
            _indicators = NavMenuIndicators.Empty;
            return;
        }

        var role = user.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var chapterIdValue = user.FindFirstValue(FonbecWebUserCustomClaim.ChapterId);
        int? chapterId = int.TryParse(chapterIdValue, out var parsedChapterId)
            ? parsedChapterId
            : null;
        var canImproveImages = UserService.HasPermission(
            null,
            role,
            DocumentPermission.DigitalImprovement,
            UserService.GetFonbecGrantsClaim(user));

        var indicators = await NavMenuService.GetAsync(role, chapterId, canImproveImages);
        if (_disposed || version != _reloadVersion)
        {
            return;
        }

        _indicators = indicators;
    }

    public void Dispose()
    {
        _disposed = true;
        NavigationManager.LocationChanged -= OnLocationChanged;
        NavMenuRefresh.Changed -= OnMenuRefresh;
    }
}