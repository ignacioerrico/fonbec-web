using Fonbec.Web.DataAccess.DataModels.Sponsors.Input;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models.Results;
using Fonbec.Web.Logic.Models.Sponsors;
using Fonbec.Web.Logic.Models.Sponsors.Input;
using Mapster;

namespace Fonbec.Web.Logic.Services;

public interface ISponsorService
{
    // estoy agregando esto:
    Task<List<SponsorsListViewModel>> GetAllSponsorsAsync();
    Task<CrudResult> CreateSponsorAsync(CreateSponsorInputModel createSponsorInputModel);
}
public class SponsorService(ISponsorRepository sponsorRepository) : ISponsorService
{
    public async Task<List<SponsorsListViewModel>> GetAllSponsorsAsync()
    {
        var allSponsorsDataModel = await sponsorRepository.GetAllSponsorsAsync();
        // Adapt is not async...
        var allSponsorsListViewModel = allSponsorsDataModel.Adapt<List<SponsorsListViewModel>>();
        return allSponsorsListViewModel;
    }
    public async Task<CrudResult> CreateSponsorAsync(CreateSponsorInputModel inputModel)
    {
        var inputDataModel = inputModel.Adapt<CreateSponsorInputDataModel>();
        var affectedRows = await sponsorRepository.CreateSponsorAsync(inputDataModel);
        return new CrudResult(affectedRows);
    }
}
