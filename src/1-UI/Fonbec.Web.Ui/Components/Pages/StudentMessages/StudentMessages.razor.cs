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
public partial class StudentMessages : AuthenticationRequiredComponentBase
{
    private StudentMessagesViewModel _model = new();
    private string _searchString = string.Empty;
    private bool _updating;

    [Inject]
    public IRecipientMessageService RecipientMessageService { get; set; } = null!;

    [Inject]
    public StudentMessageNavCount NavCount { get; set; } = null!;

    [Inject]
    public IJSRuntime JsRuntime { get; set; } = null!;

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

    private bool MatchesSearch(StudentMessageItemViewModel item) =>
        string.IsNullOrWhiteSpace(_searchString)
        || item.StudentFullName.ContainsIgnoringAccents(_searchString)
        || item.SenderName.ContainsIgnoringAccents(_searchString);

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

    private async Task ReloadAsync()
    {
        _model = await RecipientMessageService.GetForActorAsync(FonbecClaim.UserId);
    }

    private static string DeliveredLine(StudentMessageItemViewModel item) =>
        string.IsNullOrWhiteSpace(item.SharedByFullName)
            ? $"Entregado {item.SharedOnLabel}"
            : $"Entregado {item.SharedOnLabel} por {item.SharedByFullName}";

    private sealed record MessageSection(
        string Title,
        string EmptyText,
        bool Pending,
        IReadOnlyList<StudentMessageItemViewModel> Items);
}