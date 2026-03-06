using Fonbec.Web.DataAccess.DataModels.Sponsorships.Input;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models.Results;
using Fonbec.Web.Logic.Models.Sponsorships.Input;
using Mapster;

namespace Fonbec.Web.Logic.Services;

public interface ISponsorshipService
{
    Task<CrudResult> CreateSponsorshipAsync(CreateSponsorshipInputModel inputModel);
}

public class SponsorshipService(ISponsorshipRepository sponsorshipRepository) : ISponsorshipService
{
    public async Task<CrudResult> CreateSponsorshipAsync(CreateSponsorshipInputModel inputModel)
    {
        var createSponsorshipInputDataModel = inputModel.Adapt<CreateSponsorshipInputDataModel>();
        var affectedRows = await sponsorshipRepository.CreateSponsorshipAsync(createSponsorshipInputDataModel);
        return new CrudResult(affectedRows);
    }
}