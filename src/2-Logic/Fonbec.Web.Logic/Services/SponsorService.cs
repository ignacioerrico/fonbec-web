using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models.Chapters;
using Fonbec.Web.Logic.Models.Sponsors;
using Mapster;

namespace Fonbec.Web.Logic.Services;
public interface ISponsorService
{
    Task<List<SponsorsListViewModel>> GetAllSponsorsAsync();
}
public class SponsorService(ISponsorRepository sponsorRepository) : ISponsorService
{
    public async Task<List<SponsorsListViewModel>> GetAllSponsorsAsync()
    {
        var allSponsorsDataModels = await sponsorRepository.GetAllSponsorsAsync();
        var sponsorsListViewModel = allSponsorsDataModels.Adapt<List<SponsorsListViewModel>>();
        return sponsorsListViewModel;
    }
}
