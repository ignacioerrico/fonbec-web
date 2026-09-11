using Fonbec.Web.Logic.Models.Sponsorships;
using Fonbec.Web.Logic.Models.Sponsorships.Input;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.NonPages.Dialogs;

public partial class SponsorshipEditDialog
{
    private bool _saving;
    private bool _formValidationSucceeded = true;
    private bool _isEndDateKnown;
    private bool _checkingPeriod;
    private int _periodCheckVersion;
    private DateTime? _startDate;
    private DateTime? _endDate;
    private string _notes = string.Empty;
    private string _recipientName = string.Empty;
    private List<string> _lockedPlanMonthLabels = [];
    private List<DateTime> _lockedPlanStartsOn = [];
    private List<string> _exemptPlanMonthLabels = [];
    private bool _confirmExemptionRevocation;
    private SponsorshipPeriodStatus _periodStatus;

    private string PrimaryActionLabel =>
        _confirmExemptionRevocation ? "Confirmar y guardar" : "Guardar";

    private bool SaveButtonDisabled =>
        _saving
        || _checkingPeriod
        || _periodStatus == SponsorshipPeriodStatus.OverlapsExisting
        || !_formValidationSucceeded
        || !DateSelectionIsValid;

    private bool DateSelectionIsValid =>
        _startDate is DateTime start
        && (!_isEndDateKnown
            || (_endDate is DateTime end && start < end));

    private DateTime? StartMaxDate =>
        _lockedPlanStartsOn.Count == 0
            ? _endDate
            : MinDate(_lockedPlanStartsOn.Min(), _endDate);

    private DateTime? EndMinDate
    {
        get
        {
            var lockedMax = _lockedPlanStartsOn.Count == 0
                ? (DateTime?)null
                : _lockedPlanStartsOn.Max();
            if (_startDate is null)
            {
                return lockedMax;
            }

            if (lockedMax is null)
            {
                return _startDate;
            }

            return _startDate > lockedMax ? _startDate : lockedMax;
        }
    }

    [CascadingParameter]
    public IMudDialogInstance MudDialog { get; set; } = null!;

    [Parameter, EditorRequired]
    public SponsorshipsSponsorshipsListViewModel Sponsorship { get; set; } = null!;

    [Parameter, EditorRequired]
    public int UpdatedById { get; set; }

    [Inject]
    public ISponsorshipService SponsorshipService { get; set; } = null!;

    [Inject]
    public ISnackbar Snackbar { get; set; } = null!;

    protected override void OnInitialized()
    {
        _recipientName = Sponsorship.SponsorshipFullName;
        _startDate = Sponsorship.SponsorshipStartDate;
        _endDate = Sponsorship.SponsorshipEndDate;
        _isEndDateKnown = Sponsorship.SponsorshipEndDate.HasValue;
        _notes = Sponsorship.Notes;
        _lockedPlanStartsOn = [.. Sponsorship.LockedPlanStartsOn];
        _lockedPlanMonthLabels = [.. Sponsorship.LockedPlanMonthLabels];
    }

    private async Task OnIsEndDateKnownCheckBoxChanged(bool isEndDateKnown)
    {
        ResetExemptionConfirmation();
        _isEndDateKnown = isEndDateKnown;
        if (!isEndDateKnown)
        {
            _endDate = null;
        }

        await RefreshPeriodStatusAsync();
    }

    private async Task OnStartDateChanged(DateTime? startDate)
    {
        ResetExemptionConfirmation();
        _startDate = startDate is DateTime d
            ? new DateTime(d.Year, d.Month, 1)
            : null;
        await RefreshPeriodStatusAsync();
    }

    private async Task OnEndDateChanged(DateTime? endDate)
    {
        ResetExemptionConfirmation();
        _endDate = endDate is DateTime d
            ? new DateTime(d.Year, d.Month, DateTime.DaysInMonth(d.Year, d.Month))
            : null;
        await RefreshPeriodStatusAsync();
    }

    private async Task RefreshPeriodStatusAsync()
    {
        var checkVersion = ++_periodCheckVersion;
        _periodStatus = SponsorshipPeriodStatus.Available;

        if (_startDate is null || (_isEndDateKnown && _endDate is null))
        {
            _checkingPeriod = false;
            return;
        }

        _checkingPeriod = true;
        try
        {
            var status = await SponsorshipService.GetSponsorshipPeriodStatusForUpdateAsync(
                CreateInputModel());
            if (checkVersion == _periodCheckVersion)
            {
                _periodStatus = status;
            }
        }
        finally
        {
            if (checkVersion == _periodCheckVersion)
            {
                _checkingPeriod = false;
            }
        }
    }

    private UpdateSponsorshipInputModel CreateInputModel() =>
        new(
            Sponsorship.SponsorshipId,
            _startDate!.Value,
            _endDate,
            _notes,
            UpdatedById,
            _confirmExemptionRevocation);

    private async Task Save()
    {
        _saving = true;
        try
        {
            var result = await SponsorshipService.UpdateSponsorshipAsync(CreateInputModel());
            if (result.Status == UpdateSponsorshipStatus.OverlapsExisting)
            {
                _periodStatus = SponsorshipPeriodStatus.OverlapsExisting;
                Snackbar.Add(
                    "No se puede guardar porque el período se superpone con un apadrinamiento existente.",
                    Severity.Error);
                return;
            }

            if (result.Status == UpdateSponsorshipStatus.UncoversLockedPlan)
            {
                var months = result.LockedPlanMonthLabels is { Count: > 0 }
                    ? string.Join(", ", result.LockedPlanMonthLabels)
                    : "una campaña con una carta subida";
                Snackbar.Add(
                    $"No se puede dejar de cubrir {months} porque ya se subió una carta para este padrino en esa campaña.",
                    Severity.Error);
                return;
            }

            if (result.Status == UpdateSponsorshipStatus.RequiresExemptionRevocation)
            {
                _exemptPlanMonthLabels = [.. result.ExemptPlanMonthLabels ?? []];
                _confirmExemptionRevocation = true;
                return;
            }

            if (result.Status == UpdateSponsorshipStatus.NotFound || !result.AnyAffectedRows)
            {
                Snackbar.Add("No se pudo actualizar el apadrinamiento.", Severity.Error);
                return;
            }

            Snackbar.Add("El apadrinamiento fue actualizado.", Severity.Success);
            MudDialog.Close(DialogResult.Ok(true));
        }
        finally
        {
            _saving = false;
        }
    }

    private void Cancel() => MudDialog.Cancel();

    private void ResetExemptionConfirmation()
    {
        _confirmExemptionRevocation = false;
        _exemptPlanMonthLabels = [];
    }

    private static DateTime? MinDate(DateTime a, DateTime? b) =>
        b is DateTime other && other < a ? other : a;
}
