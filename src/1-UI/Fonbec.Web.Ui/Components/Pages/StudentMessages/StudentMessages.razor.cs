using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models.RecipientMessages;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Components.Layout;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.StudentMessages;

[PageMetadata(nameof(StudentMessages), "Mensajes para becarios", [FonbecRole.Uploader, FonbecRole.Manager])]
public partial class StudentMessages : AuthenticationRequiredComponentBase, IAsyncDisposable
{
    private StudentMessagesViewModel _model = new();
    private string _searchString = string.Empty;
    private bool _updating;
    private bool _loadingMore;
    private bool _hasMoreDelivered;
    private int _loadGeneration;
    private ElementReference _deliveredSentinel;
    private DotNetObjectReference<StudentMessages>? _scrollRef;

    [Inject]
    public IRecipientMessageService RecipientMessageService { get; set; } = null!;

    [Inject]
    public StudentMessageNavCount NavCount { get; set; } = null!;

    [Inject]
    public IJSRuntime JsRuntime { get; set; } = null!;

    private bool CanShare => UserRole == FonbecRole.Uploader;

    private IReadOnlyList<MessageSection> Sections =>
    [
        new("Pendientes", "No hay mensajes pendientes.", Pending: true, FilteredPending),
        new("Entregados", "No hay mensajes entregados.", Pending: false, FilteredDelivered),
    ];

    private List<StudentMessageItemViewModel> FilteredPending =>
        _model.Pending.Where(MatchesSearch).ToList();

    private List<StudentMessageItemViewModel> FilteredDelivered =>
        _model.Delivered.Where(MatchesSearch).ToList();

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        Loading = true;
        await ReloadAsync();
        Loading = false;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Loading || _loadingMore || !_hasMoreDelivered)
        {
            return;
        }

        try
        {
            _scrollRef ??= DotNetObjectReference.Create(this);
            await JsRuntime.InvokeVoidAsync("fonbecInfiniteScroll.observe", _deliveredSentinel, _scrollRef);
        }
        catch (JSException)
        {
            // The delivered list stays usable; the next render tries again.
        }
        catch (JSDisconnectedException)
        {
        }
        catch (InvalidOperationException)
        {
            // Prerender, or the sentinel is not in the document yet.
        }
    }

    private bool MatchesSearch(StudentMessageItemViewModel item) =>
        string.IsNullOrWhiteSpace(_searchString)
        || item.StudentFullName.ContainsIgnoringAccents(_searchString)
        || item.SenderName.ContainsIgnoringAccents(_searchString)
        || (item.FacilitatorFullName is not null
            && item.FacilitatorFullName.ContainsIgnoringAccents(_searchString));

    private async Task CopyAsync(StudentMessageItemViewModel item)
    {
        try
        {
            var copied = await JsRuntime.InvokeAsync<bool>("fonbecCopyText", item.CopyText);
            if (copied)
            {
                Snackbar.Add("Copiado", Severity.Success);
            }
        }
        catch (JSException)
        {
            // The message stays on the card so it can be selected when the clipboard is blocked.
        }
    }

    private Task MarkAsync(StudentMessageItemViewModel item) =>
        UpdateAsync(() => RecipientMessageService.MarkSharedAsync(item.RecipientMessageId, FonbecClaim.UserId));

    private Task UndoAsync(StudentMessageItemViewModel item) =>
        UpdateAsync(() => RecipientMessageService.UndoSharedAsync(item.RecipientMessageId, FonbecClaim.UserId));

    private async Task UpdateAsync(Func<Task<bool>> update)
    {
        if (_updating)
        {
            return;
        }

        _updating = true;
        try
        {
            var saved = await update();
            if (!saved)
            {
                Snackbar.Add("No se pudo actualizar el mensaje.", Severity.Error);
                return;
            }

            await ReloadAsync();
            NavCount.NotifyChanged();
        }
        finally
        {
            _updating = false;
        }
    }

    [JSInvokable]
    public async Task LoadMoreDeliveredAsync()
    {
        if (_loadingMore || _updating || !_hasMoreDelivered || Loading)
        {
            return;
        }

        _loadingMore = true;
        var generation = _loadGeneration;
        var skip = _model.Delivered.Count;
        try
        {
            await InvokeAsync(StateHasChanged);
            var page = await RecipientMessageService.GetMoreDeliveredAsync(FonbecClaim.UserId, skip);
            if (generation != _loadGeneration)
            {
                return;
            }

            var seen = _model.Delivered.Select(item => item.RecipientMessageId).ToHashSet();
            _model.Delivered.AddRange(page.Items.Where(item => seen.Add(item.RecipientMessageId)));
            _hasMoreDelivered = page.HasMore;
        }
        finally
        {
            if (generation == _loadGeneration)
            {
                _loadingMore = false;
            }

            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task ReloadAsync()
    {
        var generation = ++_loadGeneration;
        _loadingMore = false;
        var model = await RecipientMessageService.GetForActorAsync(FonbecClaim.UserId);
        if (generation != _loadGeneration)
        {
            return;
        }

        _model = model;
        _hasMoreDelivered = model.HasMoreDelivered;
    }

    public async ValueTask DisposeAsync()
    {
        _loadGeneration++;
        try
        {
            await JsRuntime.InvokeVoidAsync("fonbecInfiniteScroll.disconnect", _deliveredSentinel);
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or InvalidOperationException)
        {
        }

        _scrollRef?.Dispose();
    }

    private static string ResponsibleFacilitatorLine(StudentMessageItemViewModel item) =>
        string.IsNullOrWhiteSpace(item.FacilitatorFullName)
            ? "Sin mediador responsable de compartirlo."
            : $"Mediador responsable de compartirlo: {item.FacilitatorFullName}";

    private sealed record MessageSection(
        string Title,
        string EmptyText,
        bool Pending,
        IReadOnlyList<StudentMessageItemViewModel> Items);
}