using Fonbec.Web.DataAccess.DataModels.Sponsors.Input;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Constants;
using Fonbec.Web.Logic.Models;
using Fonbec.Web.Logic.Models.Results;
using Fonbec.Web.Logic.Models.Sponsors;
using Fonbec.Web.Logic.Models.Sponsors.Input;
using Mapster;

namespace Fonbec.Web.Logic.Services;

public interface ISponsorService
{
    // estoy agregando esto:
    Task<List<SponsorsListViewModel>> GetAllSponsorsAsync(int? chapterId = null);
    Task<CrudResult> CreateSponsorAsync(CreateSponsorInputModel createSponsorInputModel);

    // add
    Task<List<SelectableModel<int>>> GetAllSponsorsForSelectionAsync();
}
public class SponsorService(ISponsorRepository sponsorRepository) : ISponsorService
{
    public async Task<List<SponsorsListViewModel>> GetAllSponsorsAsync(int? chapterId = null)
    {
        // parameter added
        var allSponsorsDataModel = await sponsorRepository.GetAllSponsorsAsync(chapterId);
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

    // add
    public async Task<List<SelectableModel<int>>> GetAllSponsorsForSelectionAsync()
    {
        var sponsors = await GetAllSponsorsAsync();

        return sponsors
            .Select(s => new SelectableModel<int>(s.SponsorId, s.SponsorFirstName))
            .ToList();
    }
}
