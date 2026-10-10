using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.DataModels.Documents;
using Fonbec.Web.DataAccess.DataModels.LetterFollowUp;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models.Navigation;

namespace Fonbec.Web.Logic.Services;

public interface INavMenuService
{
    Task<NavMenuIndicators> GetAsync(string role, int? chapterId, bool canImproveImages);
}

public sealed class NavMenuService(
    IStudentRepository studentRepository,
    ISponsorRepository sponsorRepository,
    ICompanyRepository companyRepository,
    ILetterFollowUpRepository letterFollowUpRepository,
    IPlannedDeliveryRepository plannedDeliveryRepository,
    ILetterPlanProgressService letterPlanProgressService,
    IDocumentRepository documentRepository) : INavMenuService
{
    public async Task<NavMenuIndicators> GetAsync(string role, int? chapterId, bool canImproveImages)
    {
        Task<int>? students = null;
        Task<int>? sponsors = null;
        Task<int>? companies = null;
        Task<int?>? campaignPercent = null;
        Task<OpenFlagCounts>? flags = null;
        Task<ReviewProgressDataModel>? progress = null;

        var pending = new List<Task>();

        if (role is FonbecRole.Admin or FonbecRole.Manager)
        {
            students = studentRepository.CountStudentsAsync(chapterId);
            sponsors = sponsorRepository.CountSponsorsAsync(chapterId);
            pending.Add(students);
            pending.Add(sponsors);
        }

        if (role == FonbecRole.Manager)
        {
            companies = companyRepository.CountCompaniesAsync();
            pending.Add(companies);
        }

        if (role == FonbecRole.Manager && chapterId is int managerChapterId)
        {
            campaignPercent = GetCampaignCompletionPercentAsync(managerChapterId);
            flags = letterFollowUpRepository.CountOpenFlagsAsync(managerChapterId);
            pending.Add(campaignPercent);
            pending.Add(flags);
        }

        if (role is FonbecRole.Reviewer or FonbecRole.Manager)
        {
            progress = documentRepository.GetGlobalReviewProgressAsync(null);
            pending.Add(progress);
        }

        if (pending.Count > 0)
        {
            await Task.WhenAll(pending);
        }

        int? pendingReview = null;
        int? pendingImprovement = null;
        if (progress is not null)
        {
            var review = await progress;
            pendingReview = review.PendingLetters + review.PendingReportCards + review.PendingOther;
            if (canImproveImages)
            {
                pendingImprovement = review.PendingImprovement;
            }
        }

        var openFlags = flags is null
            ? default
            : await flags;

        return new NavMenuIndicators
        {
            StudentCount = students is null ? null : await students,
            SponsorCount = sponsors is null ? null : await sponsors,
            CompanyCount = companies is null ? null : await companies,
            CampaignCompletionPercent = campaignPercent is null ? null : await campaignPercent,
            RedFlags = openFlags.RedFlags,
            GreenFlags = openFlags.GreenFlags,
            PendingImprovement = pendingImprovement,
            PendingReview = pendingReview,
        };
    }

    private async Task<int?> GetCampaignCompletionPercentAsync(int chapterId)
    {
        var plan = await plannedDeliveryRepository.GetCurrentPlanAsync(chapterId);
        if (plan is null)
        {
            return null;
        }

        var progress = await letterPlanProgressService.GetProgressAsync(plan.PlannedDeliveryId, chapterId);
        return progress is null
            ? null
            : decimal.ToInt32(progress.Summary.CompletionPercent);
    }
}