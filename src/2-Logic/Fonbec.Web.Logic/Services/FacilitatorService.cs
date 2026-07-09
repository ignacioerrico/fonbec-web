using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models.Facilitators;
using Mapster;

namespace Fonbec.Web.Logic.Services;

public interface IFacilitatorService
{
    Task<StudentsDashboardViewModel> GetStudentsDashboardAsync(int facilitatorId);
}

public class FacilitatorService(IFacilitatorRepository facilitatorRepository) : IFacilitatorService
{
    public async Task<StudentsDashboardViewModel> GetStudentsDashboardAsync(int facilitatorId)
    {
        var studentsDataModel = await facilitatorRepository.GetActiveSponsoredStudentsAsync(facilitatorId);
        var currentPlan = await facilitatorRepository.GetCurrentPlanForFacilitatorAsync(facilitatorId);

        return new StudentsDashboardViewModel
        {
            CurrentPlanId = currentPlan?.PlanId,
            CurrentPlanStartsOn = currentPlan?.StartsOn,
            Students = studentsDataModel.Adapt<List<FacilitatorStudentsListViewModel>>(),
        };
    }
}