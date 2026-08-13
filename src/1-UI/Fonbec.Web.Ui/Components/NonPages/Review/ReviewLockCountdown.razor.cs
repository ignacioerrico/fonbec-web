using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.NonPages.Review;

public sealed partial class ReviewLockCountdown : IDisposable
{
    [Parameter, EditorRequired]
    public DateTime ExpiresAtUtc { get; set; }

    [Parameter]
    public EventCallback OnExpired { get; set; }

    private const int IntervalInMilliseconds = 1000; // 1 second

    private System.Timers.Timer? _timer;
    private TimeSpan _remaining;
    private bool _expiredFired;

    protected override void OnInitialized()
    {
        UpdateRemaining();

        // Initialize the timer to tick every IntervalInMilliseconds.
        _timer = new System.Timers.Timer(IntervalInMilliseconds) { AutoReset = true };
        _timer.Elapsed += async (_, _) => await OnTickAsync();
        _timer.Start();
    }

    private async Task OnTickAsync()
    {
        UpdateRemaining();

        var justExpired = _remaining <= TimeSpan.Zero && !_expiredFired;
        if (justExpired)
        {
            _expiredFired = true;
            _timer?.Stop();
        }

        await InvokeAsync(async () =>
        {
            StateHasChanged();
            if (justExpired)
            {
                await OnExpired.InvokeAsync();
            }
        });
    }

    private void UpdateRemaining()
    {
        var remaining = ExpiresAtUtc - DateTime.UtcNow;
        _remaining = remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    private string Display => $"{(int)_remaining.TotalMinutes:00}:{_remaining.Seconds:00}";

    private Color ChipColor => _remaining <= TimeSpan.FromMinutes(5)
        ? Color.Error
        : _remaining <= TimeSpan.FromMinutes(10)
            ? Color.Warning
            : Color.Success;

    public void Dispose() => _timer?.Dispose();
}