using Fonbec.Web.DataAccess.DataModels.Facilitators;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models.Facilitators;
using Fonbec.Web.Logic.Models.Students;
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
            var letterStatuses = await facilitatorRepository.GetCurrentLetterStatusesAsync(
                currentPlan.PlanId, students.Select(s => s.StudentId).ToList());

            foreach (var student in students)
            {
                student.IsLetterExemptForCurrentPlan = exemptStudentIds.Contains(student.StudentId);
                student.LetterStatuses = BuildLetterStatuses(student.StudentId, student.Sponsors, letterStatuses);
                student.LetterAggregate = LetterAggregation.Aggregate(
                    hasActivePlan: true,
                    isExempt: student.IsLetterExemptForCurrentPlan,
                    slotStatuses: student.LetterStatuses.Select(s => s.Status).ToList());
            }
        }
        else
        {
            foreach (var student in students)
            {
                student.LetterAggregate = LetterAggregateStatus.NoPlan;
            }
        }

        return new StudentsDashboardViewModel
        {
            CurrentPlanId = currentPlan?.PlanId,
            CurrentPlanStartsOn = currentPlan?.StartsOn,
            Students = students,
        };
    }

    private static List<SponsorLetterStatusViewModel> BuildLetterStatuses(int studentId, List<DashboardSponsorViewModel> sponsors, List<SponsorLetterStatusDataModel> letterStatuses)
    {
        return sponsors.Select(sponsor =>
        {
            var currentLetter = letterStatuses.SingleOrDefault(l =>
                l.StudentId == studentId
                && l.SponsorId == sponsor.SponsorId
                && l.CompanyId == sponsor.CompanyId);

            var status = LetterAggregation.ToSlotStatus(currentLetter?.Status);

            return new SponsorLetterStatusViewModel
            {
                SponsorshipId = sponsor.SponsorshipId,
                SponsorId = sponsor.SponsorId,
                CompanyId = sponsor.CompanyId,
                IsCompany = sponsor.IsCompany,
                RecipientName = sponsor.RecipientName,
                Status = status,
                RejectionReason = status == LetterSlotStatus.Rejected ? currentLetter?.RejectionReason : null,
            };
        }).ToList();
    }
}