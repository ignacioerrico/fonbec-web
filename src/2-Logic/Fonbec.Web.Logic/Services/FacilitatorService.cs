using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models.Facilitators;
using Mapster;

namespace Fonbec.Web.Logic.Services;

public interface IFacilitatorService
{
    Task<StudentsDashboardViewModel> GetStudentsDashboardAsync(int facilitatorId);
}

public class FacilitatorService(
    IFacilitatorRepository facilitatorRepository,
    ILetterExemptionService letterExemptionService) : IFacilitatorService
{
    public async Task<StudentsDashboardViewModel> GetStudentsDashboardAsync(int facilitatorId)
    {
        var studentsDataModel = await facilitatorRepository.GetActiveSponsoredStudentsAsync(facilitatorId);
        var currentPlan = await facilitatorRepository.GetCurrentPlanForFacilitatorAsync(facilitatorId);

        var students = studentsDataModel.Adapt<List<FacilitatorStudentsListViewModel>>();

        if (currentPlan is not null)
        {
            var exemptStudentIds = await letterExemptionService.GetExemptStudentIdsForPlanAsync(currentPlan.PlanId);
            foreach (var student in students)
            {
                student.IsLetterExemptForCurrentPlan = exemptStudentIds.Contains(student.StudentId);
            }
        }

        return new StudentsDashboardViewModel
        {
            CurrentPlanId = currentPlan?.PlanId,
            CurrentPlanStartsOn = currentPlan?.StartsOn,
            Students = students,
        };
    }
}