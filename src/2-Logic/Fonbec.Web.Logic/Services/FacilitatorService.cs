using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models.Facilitators;
using Mapster;

namespace Fonbec.Web.Logic.Services;

public interface IFacilitatorService
{
    Task<List<FacilitatorStudentsListViewModel>> GetActiveSponsoredStudentsAsync(int facilitatorId);

    Task<MisBecariosDashboardViewModel> GetMisBecariosDashboardAsync(int facilitatorId);
}

public class FacilitatorService(IFacilitatorRepository facilitatorRepository) : IFacilitatorService
{
    public async Task<List<FacilitatorStudentsListViewModel>> GetActiveSponsoredStudentsAsync(int facilitatorId)
    {
        var studentsDataModel = await facilitatorRepository.GetActiveSponsoredStudentsAsync(facilitatorId);
        return studentsDataModel.Adapt<List<FacilitatorStudentsListViewModel>>();
    }

    public async Task<MisBecariosDashboardViewModel> GetMisBecariosDashboardAsync(int facilitatorId)
    {
        var dashboardDataModel = await facilitatorRepository.GetMisBecariosDashboardAsync(facilitatorId);
        return dashboardDataModel.Adapt<MisBecariosDashboardViewModel>();
    }
}