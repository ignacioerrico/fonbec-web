using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.Models.Documents;
using Fonbec.Web.Logic.Models.Documents.Input;
using Fonbec.Web.Logic.Models.Results;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.NonPages.Documents;

/// <summary>
/// Minimal review panel for a locked <see cref="DocumentType.Other"/> document: approve, or reject
/// with a required reason (and required notes when the reason is "Otro"). No confirmations, no
/// assessment. Rendered by the (US 116) reviewer workspace between the content viewer and the
/// action bar; the content viewer itself is the workspace's responsibility, not this panel's.
/// </summary>
public partial class OtherReviewPanel : ComponentBase
{
    private List<RejectedReasonViewModel> _reasons = [];
    private int? _selectedReasonId;
    private string? _rejectionNotes;
    private bool _saving;

    [Parameter]
    [EditorRequired]
    public long DocumentId { get; set; }

    [Parameter]
    [EditorRequired]
    public byte[] RowVersion { get; set; } = null!;

    [Parameter]
    [EditorRequired]
    public int ReviewerId { get; set; }

    [Parameter]
    [EditorRequired]
    public string ReviewerRole { get; set; } = string.Empty;

    [Parameter]
    public bool Expired { get; set; }

    [Parameter]
    public EventCallback OnCompleted { get; set; }

    [Inject]
    public IDocumentService DocumentService { get; set; } = null!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = null!;

    private RejectedReasonViewModel? SelectedReason =>
        _reasons.SingleOrDefault(r => r.RejectedReasonId == _selectedReasonId);

    private bool ApproveDisabled => _saving || Expired;

    private bool RejectDisabled =>
        _saving
        || Expired
        || _selectedReasonId is null
        || (SelectedReason?.RequiresNotes == true && string.IsNullOrWhiteSpace(_rejectionNotes))
        || (_rejectionNotes?.Length > MaxLength.Document.RejectionNotes);

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        _reasons = await DocumentService.GetApplicableRejectedReasonsAsync(DocumentType.Other);
    }

    private async Task Approve()
    {
        if (_saving)
        {
            return;
        }

        _saving = true;
        StateHasChanged();
        ReviewResult result;
        try
        {
            result = await DocumentService.ApproveOtherDocumentAsync(new ApproveOtherDocumentInputModel(
                DocumentId, ReviewerId, ReviewerRole, RowVersion));
        }
        finally
        {
            _saving = false;
        }

        if (ShowResult(result, "Documento aprobado."))
        {
            await OnCompleted.InvokeAsync();
        }
    }

    private async Task Reject()
    {
        if (_saving)
        {
            return;
        }

        _saving = true;
        StateHasChanged();
        ReviewResult result;
        try
        {
            result = await DocumentService.RejectOtherDocumentAsync(new RejectOtherDocumentInputModel(
                DocumentId, ReviewerId, ReviewerRole, RowVersion, _selectedReasonId, NormalizedNotes()));
        }
        finally
        {
            _saving = false;
        }

        if (ShowResult(result, "Documento rechazado."))
        {
            await OnCompleted.InvokeAsync();
        }
    }

    private bool ShowResult(ReviewResult result, string successMessage)
    {
        if (!result.IsSuccess)
        {
            if (result.Errors is null or { Count: 0 })
            {
                Snackbar.Add("No se pudo guardar la revisión.", Severity.Error);
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    Snackbar.Add(error, Severity.Error);
                }
            }

            return false;
        }

        Snackbar.Add(successMessage, Severity.Success);
        return true;
    }

    private string? NormalizedNotes() =>
        string.IsNullOrWhiteSpace(_rejectionNotes) ? null : _rejectionNotes.Trim();
}
