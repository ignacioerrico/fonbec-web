using System.Security.Claims;
using Fonbec.Web.Logic.Models.RecipientMessages;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;

namespace Fonbec.Web.Ui.Components.Layout;

public partial class StudentMessagesNavLink : ComponentBase, IDisposable
{
    private int _pendingCount;
    private string? _badge;
    private bool _disposed;

    [Inject]
    public IRecipientMessageService RecipientMessageService { get; set; } = null!;

    [Inject]
    public StudentMessageNavCount NavCount { get; set; } = null!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = null!;

    [CascadingParameter]
    private Task<AuthenticationState>? AuthenticationState { get; set; }

    protected override async Task OnInitializedAsync()
    {
        NavCount.Changed += OnCountChanged;
        NavigationManager.LocationChanged += OnLocationChanged;
        await ReloadAsync();
    }

    private void OnCountChanged() => _ = InvokeAsync(ReloadAsync);

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e) =>
        _ = InvokeAsync(ReloadAsync);

    private async Task ReloadAsync()
    {
        var userId = await GetUserIdAsync();
        if (_disposed || userId is null)
        {
            return;
        }

        _pendingCount = await RecipientMessageService.CountPendingForActorAsync(userId.Value);
        if (_disposed)
        {
            return;
        }

        _badge = PendingMessageBadge.Format(_pendingCount);
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
        NavCount.Changed -= OnCountChanged;
        NavigationManager.LocationChanged -= OnLocationChanged;
    }
}