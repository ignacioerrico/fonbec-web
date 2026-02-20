using Fonbec.Web.DataAccess.DataModels.Sponsors.Input;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models;
using Fonbec.Web.Logic.Models.Results;
using Fonbec.Web.Logic.Models.Sponsors;
using Fonbec.Web.Logic.Models.Sponsors.Input;
using Mapster;

namespace Fonbec.Web.Logic.Services;

public interface ISponsorService
{
    Task<List<SponsorsListViewModel>> GetAllSponsorsAsync(int? chapterId);
    Task<CrudResult> CreateSponsorAsync(CreateSponsorInputModel createSponsorInputModel);
    Task<List<SelectableModel<int>>> GetAllSponsorsForSelectionAsync();
}

public class SponsorService(ISponsorRepository sponsorRepository) : ISponsorService
{
    public async Task<List<SponsorsListViewModel>> GetAllSponsorsAsync(int? chapterId)
    {
        var allSponsorsDataModel = await sponsorRepository.GetAllSponsorsAsync(chapterId);
        var allSponsorsListViewModel = allSponsorsDataModel.Adapt<List<SponsorsListViewModel>>();
        return allSponsorsListViewModel;
    }

    public async Task<CrudResult> CreateSponsorAsync(CreateSponsorInputModel inputModel)
    {
        var inputDataModel = inputModel.Adapt<CreateSponsorInputDataModel>();
        var affectedRows = await sponsorRepository.CreateSponsorAsync(inputDataModel);
        return new CrudResult(affectedRows);
    }

    public async Task<List<SelectableModel<int>>> GetAllSponsorsForSelectionAsync()
    {
        //return await GetAllSponsorsAsync()
        //    .ContinueWith(s => s.Result.Adapt<List<SelectableModel<int>>>());
        return null; 
    }
}
