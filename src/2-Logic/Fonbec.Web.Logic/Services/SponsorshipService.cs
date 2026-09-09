using Fonbec.Web.DataAccess.DataModels.Sponsorships.Input;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models.Sponsorships;
using Fonbec.Web.Logic.Models.Sponsorships.Input;
using Mapster;

namespace Fonbec.Web.Logic.Services;

public interface ISponsorshipService
{
    Task<SponsorshipsListViewModel> GetAllSponsorshipsAsync(int studentId);
    Task<SponsorshipPeriodStatus> GetSponsorshipPeriodStatusAsync(
        CreateSponsorshipInputModel inputModel);
    Task<CreateSponsorshipResult> CreateSponsorshipAsync(CreateSponsorshipInputModel inputModel);
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

    public async Task<CreateSponsorshipResult> CreateSponsorshipAsync(
        CreateSponsorshipInputModel inputModel)
    {
        var createSponsorshipInputDataModel = inputModel.Adapt<CreateSponsorshipInputDataModel>();
        var result = await sponsorshipRepository.CreateSponsorshipAsync(createSponsorshipInputDataModel);
        return new CreateSponsorshipResult(result.AffectedRows, MapPeriodStatus(result.PeriodMatch));
    }

    private static SponsorshipPeriodStatus MapPeriodStatus(SponsorshipPeriodMatch match) =>
        match switch
        {
            SponsorshipPeriodMatch.Overlap => SponsorshipPeriodStatus.OverlapsExisting,
            SponsorshipPeriodMatch.Adjacent => SponsorshipPeriodStatus.ExtendsExisting,
            _ => SponsorshipPeriodStatus.Available,
        };
}