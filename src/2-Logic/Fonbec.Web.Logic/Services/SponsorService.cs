using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fonbec.Web.DataAccess.DataModels.Sponsors.Input;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models.Results;
using Fonbec.Web.Logic.Models.Sponsors.Input;
using Mapster;

namespace Fonbec.Web.Logic.Services;

public interface ISponsorService
{
    Task<CrudResult> CreateSponsorAsync(CreateSponsorInputModel createSponsorInputModel);
}
public class SponsorService(ISponsorRepository sponsorRepository) : ISponsorService
{
    public async Task<CrudResult> CreateSponsorAsync(CreateSponsorInputModel inputModel)
    {
        var inputDataModel = inputModel.Adapt<CreateSponsorInputDataModel>();
        var affectedRows = await sponsorRepository.CreateSponsorAsync(inputDataModel);
        return new CrudResult(affectedRows);
    }
}
