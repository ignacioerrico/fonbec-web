using System.Security.Claims;
using Fonbec.Web.Logic.Models.RecipientMessages;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;

namespace Fonbec.Web.Ui.Components.Layout;

public sealed partial class FacilitatorStudentsNavLink : ComponentBase, IDisposable
{
    private string? _badge;
    private bool _disposed;

    [Inject]
    public IFacilitatorService FacilitatorService { get; set; } = null!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = null!;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationState { get; set; }

    protected override async Task OnInitializedAsync()
    {
        NavigationManager.LocationChanged += OnLocationChanged;
        await ReloadAsync();
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e) =>
        _ = InvokeAsync(ReloadAsync);

    private async Task ReloadAsync()
    {
        var userId = await GetUserIdAsync();
        if (_disposed || userId is null)
        {
            return;
        }

        var pending = await FacilitatorService.CountPendingLettersAsync(userId.Value);
        if (_disposed)
        {
            return;
        }

        _badge = PendingMessageBadge.Format(pending);
    }

    private async Task<int?> GetUserIdAsync()
    {
        if (AuthenticationState is null)
        {
            return null;
        }

        var user = (await AuthenticationState).User;
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userId, out var parsed) ? parsed : null;
    }

    public void Dispose()
    {
        _disposed = true;
        NavigationManager.LocationChanged -= OnLocationChanged;
    }
}