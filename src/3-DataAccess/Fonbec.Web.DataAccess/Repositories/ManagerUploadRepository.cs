using Fonbec.Web.DataAccess.DataModels.Facilitators;
using Fonbec.Web.DataAccess.DataModels.Managers;
using Fonbec.Web.DataAccess.Queries;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Repositories;

public interface IManagerUploadRepository
{
    Task<ManagerUploadContextDataModel?> GetUploadContextAsync(
        int studentId, int? planId, int? sponsorId, int? companyId);

    Task<List<ManagerLetterRecipientOptionDataModel>> GetActiveSponsorshipsAsync(int studentId, DateTime asOf);

    Task<CurrentPlanDataModel?> GetCurrentPlanForChapterAsync(int chapterId);
}

public class ManagerUploadRepository(
    IDbContextFactory<FonbecWebDbContext> dbContext,
    TimeProvider timeProvider) : IManagerUploadRepository
{
    public async Task<ManagerUploadContextDataModel?> GetUploadContextAsync(
        int studentId, int? planId, int? sponsorId, int? companyId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var student = await db.Students
            .AsNoTracking()
            .Where(s => s.Id == studentId && !s.IsDeleted)
            .Select(s => new
            {
                s.Id,
                s.FirstName,
                s.LastName,
                s.ChapterId,
                s.IsActive,
                FacilitatorFirstName = s.Facilitator.FirstName,
                FacilitatorLastName = s.Facilitator.LastName,
                s.SecondarySchoolStartYear,
                s.UniversityStartYear,
            })
            .FirstOrDefaultAsync();

        if (student is null)
        {
            return null;
        }

        // Only resolve a plan that is genuinely valid for this student's chapter, matching the
        // server-side create check; an invalid plan yields a null start date so the upload
        // context resolution fails instead of rendering a form that would be rejected on submit.
        DateTime? planStartsOn = null;
        if (planId.HasValue)
        {
            planStartsOn = await CampaignQueries.SelectStartsOnAsync(
                CampaignQueries.Open(db.PlannedDeliveries.AsNoTracking())
                    .Where(p => p.Id == planId.Value
                                && (p.ChapterId == null || p.ChapterId == student.ChapterId)));
        }

        // A letter is for the plan's start date. Other uploads have no plan, so they use today.
        var asOf = planStartsOn ?? utcNow;

        // Resolve the recipient name only from an active sponsorship with the student, so an
        // unrelated or inactive sponsor/company does not produce a renderable letter context.
        string? sponsorFirstName = null;
        string? sponsorLastName = null;
        if (sponsorId.HasValue)
        {
            var sponsor = await db.Sponsorships
                .AsNoTracking()
                .Where(CampaignQueries.Covers(asOf))
                .Where(sp => sp.StudentId == studentId && sp.SponsorId == sponsorId.Value)
                .Select(sp => new { sp.Sponsor!.FirstName, sp.Sponsor.LastName })
                .FirstOrDefaultAsync();
            sponsorFirstName = sponsor?.FirstName;
            sponsorLastName = sponsor?.LastName;
        }

        string? companyName = null;
        if (companyId.HasValue)
        {
            companyName = await db.Sponsorships
                .AsNoTracking()
                .Where(CampaignQueries.Covers(asOf))
                .Where(sp => sp.StudentId == studentId && sp.CompanyId == companyId.Value)
                .Select(sp => sp.Company!.Name)
                .FirstOrDefaultAsync();
        }

        return new ManagerUploadContextDataModel
        {
            StudentId = student.Id,
            StudentFirstName = student.FirstName,
            StudentLastName = student.LastName,
            ChapterId = student.ChapterId,
            IsActive = student.IsActive,
            FacilitatorFirstName = student.FacilitatorFirstName,
            FacilitatorLastName = student.FacilitatorLastName,
            SecondarySchoolStartYear = student.SecondarySchoolStartYear,
            UniversityStartYear = student.UniversityStartYear,
            PlanStartsOn = planStartsOn,
            SponsorFirstName = sponsorFirstName,
            SponsorLastName = sponsorLastName,
            CompanyName = companyName,
        };
    }

    public async Task<List<ManagerLetterRecipientOptionDataModel>> GetActiveSponsorshipsAsync(int studentId, DateTime asOf)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        return await db.Sponsorships
            .AsNoTracking()
            .Where(sp => sp.StudentId == studentId)
            .Where(CampaignQueries.Covers(asOf))
            .Select(sp => new ManagerLetterRecipientOptionDataModel
            {
                SponsorId = sp.SponsorId,
                CompanyId = sp.CompanyId,
                RecipientName = sp.CompanyId != null && sp.Company != null
                    ? sp.Company.Name
                    : sp.Sponsor != null
                        ? sp.Sponsor.FirstName + " " + sp.Sponsor.LastName
                        : string.Empty,
            })
            .ToListAsync();
    }

    public async Task<CurrentPlanDataModel?> GetCurrentPlanForChapterAsync(int chapterId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        return await CampaignQueries.SelectCurrentAsync(
            CampaignQueries.OpenForChapter(db.PlannedDeliveries.AsNoTracking(), chapterId));
    }
}