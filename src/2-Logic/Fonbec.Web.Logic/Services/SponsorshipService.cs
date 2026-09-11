using Fonbec.Web.DataAccess.DataModels.Sponsorships.Input;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models.Sponsorships;
using Fonbec.Web.Logic.Models.Sponsorships.Input;
using Mapster;

namespace Fonbec.Web.Logic.Services;

public interface ISponsorshipService
{
    Task<SponsorshipsListViewModel> GetAllSponsorshipsAsync(int studentId);
    Task<SponsorshipPeriodStatus> GetSponsorshipPeriodStatusAsync(
        CreateSponsorshipInputModel inputModel);
    Task<SponsorshipPeriodStatus> GetSponsorshipPeriodStatusForUpdateAsync(
        UpdateSponsorshipInputModel inputModel);
    Task<CreateSponsorshipResult> CreateSponsorshipAsync(CreateSponsorshipInputModel inputModel);
    Task<UpdateSponsorshipResult> UpdateSponsorshipAsync(UpdateSponsorshipInputModel inputModel);
}

public class SponsorshipService(ISponsorshipRepository sponsorshipRepository) : ISponsorshipService
{
    public async Task<SponsorshipsListViewModel> GetAllSponsorshipsAsync(int studentId)
    {
        var allSponsorshipsDataModel = await sponsorshipRepository.GetAllSponsorshipsAsync(studentId);
        var allSponsorshipListViewModel = allSponsorshipsDataModel.Adapt<SponsorshipsListViewModel>();
        return allSponsorshipListViewModel;
    }

    public async Task<SponsorshipPeriodStatus> GetSponsorshipPeriodStatusAsync(
        CreateSponsorshipInputModel inputModel)
    {
        var createSponsorshipInputDataModel = inputModel.Adapt<CreateSponsorshipInputDataModel>();
        var match = await sponsorshipRepository.GetSponsorshipPeriodMatchAsync(
            createSponsorshipInputDataModel);
        return MapPeriodStatus(match);
    }

    public async Task<SponsorshipPeriodStatus> GetSponsorshipPeriodStatusForUpdateAsync(
        UpdateSponsorshipInputModel inputModel)
    {
        var updateInputDataModel = inputModel.Adapt<UpdateSponsorshipInputDataModel>();
        var match = await sponsorshipRepository.GetSponsorshipPeriodMatchForUpdateAsync(
            updateInputDataModel);
        return MapPeriodStatus(match);
    }

    public async Task<CreateSponsorshipResult> CreateSponsorshipAsync(
        CreateSponsorshipInputModel inputModel)
    {
        var createSponsorshipInputDataModel = inputModel.Adapt<CreateSponsorshipInputDataModel>();
        var result = await sponsorshipRepository.CreateSponsorshipAsync(createSponsorshipInputDataModel);
        var completedLabels = (result.CompletedPlanStartsOn ?? [])
            .Select(d => d.ToSpanishMonthYear())
            .ToList();
        return new CreateSponsorshipResult(
            result.AffectedRows,
            MapPeriodStatus(result.PeriodMatch),
            completedLabels);
    }

    public async Task<UpdateSponsorshipResult> UpdateSponsorshipAsync(
        UpdateSponsorshipInputModel inputModel)
    {
        var updateInputDataModel = inputModel.Adapt<UpdateSponsorshipInputDataModel>();
        var result = await sponsorshipRepository.UpdateSponsorshipAsync(updateInputDataModel);
        var labels = (result.UncoveredPlanStartsOn ?? [])
            .Select(d => d.ToSpanishMonthYear())
            .ToList();
        var exemptionLabels = (result.ExemptPlanStartsOn ?? [])
            .Select(d => d.ToSpanishMonthYear())
            .ToList();
        var completedLabels = (result.CompletedPlanStartsOn ?? [])
            .Select(d => d.ToSpanishMonthYear())
            .ToList();
        return new UpdateSponsorshipResult(
            result.AffectedRows,
            MapUpdateStatus(result.Outcome),
            labels,
            exemptionLabels,
            completedLabels);
    }

    private static SponsorshipPeriodStatus MapPeriodStatus(SponsorshipPeriodMatch match) =>
        match switch
        {
            SponsorshipPeriodMatch.Overlap => SponsorshipPeriodStatus.OverlapsExisting,
            SponsorshipPeriodMatch.Adjacent => SponsorshipPeriodStatus.ExtendsExisting,
            _ => SponsorshipPeriodStatus.Available,
        };

    private static UpdateSponsorshipStatus MapUpdateStatus(UpdateSponsorshipOutcome outcome) =>
        outcome switch
        {
            UpdateSponsorshipOutcome.Overlap => UpdateSponsorshipStatus.OverlapsExisting,
            UpdateSponsorshipOutcome.UncoversLockedPlan => UpdateSponsorshipStatus.UncoversLockedPlan,
            UpdateSponsorshipOutcome.RequiresExemptionRevocation =>
                UpdateSponsorshipStatus.RequiresExemptionRevocation,
            UpdateSponsorshipOutcome.AddsSlotToCompletedPlan =>
                UpdateSponsorshipStatus.AddsSlotToCompletedPlan,
            UpdateSponsorshipOutcome.NotFound => UpdateSponsorshipStatus.NotFound,
            _ => UpdateSponsorshipStatus.Saved,
        };
}
