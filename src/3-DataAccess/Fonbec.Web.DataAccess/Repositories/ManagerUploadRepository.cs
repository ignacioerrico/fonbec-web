using Fonbec.Web.DataAccess.DataModels.Facilitators;
using Fonbec.Web.DataAccess.DataModels.Managers;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Repositories;

public interface IManagerUploadRepository
{
    Task<ManagerUploadContextDataModel?> GetUploadContextAsync(
        int studentId, int? planId, int? sponsorId, int? companyId);

    Task<List<ManagerLetterRecipientOptionDataModel>> GetActiveSponsorshipsAsync(int studentId);

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
            planStartsOn = await db.PlannedDeliveries
                .AsNoTracking()
                .Where(p => p.Id == planId.Value
                            && p.IsActive
                            && !p.Completed
                            && (p.ChapterId == null || p.ChapterId == student.ChapterId))
                .Select(p => (DateTime?)p.StartsOn)
                .FirstOrDefaultAsync();
        }

        // Resolve the recipient name only from an active sponsorship with the student, so an
        // unrelated or inactive sponsor/company does not produce a renderable letter context.
        string? sponsorFirstName = null;
        string? sponsorLastName = null;
        if (sponsorId.HasValue)
        {
            var sponsor = await db.Sponsorships
                .AsNoTracking()
                .Where(sp => sp.StudentId == studentId
                             && sp.SponsorId == sponsorId.Value
                             && sp.IsActive
                             && sp.StartDate <= utcNow
                             && (sp.EndDate == null || sp.EndDate >= utcNow)
                             && sp.Sponsor != null
                             && sp.Sponsor.IsActive
                             && !sp.Sponsor.IsDeleted)
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
                .Where(sp => sp.StudentId == studentId
                             && sp.CompanyId == companyId.Value
                             && sp.IsActive
                             && sp.StartDate <= utcNow
                             && (sp.EndDate == null || sp.EndDate >= utcNow)
                             && sp.Company != null
                             && sp.Company.IsActive)
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

    public async Task<List<ManagerLetterRecipientOptionDataModel>> GetActiveSponsorshipsAsync(int studentId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        return await db.Sponsorships
            .AsNoTracking()
            .Where(sp => sp.StudentId == studentId
                         && sp.IsActive
                         && sp.StartDate <= utcNow
                         && (sp.EndDate == null || sp.EndDate >= utcNow)
                         && (
                             (sp.SponsorId != null
                              && sp.Sponsor != null
                              && sp.Sponsor.IsActive
                              && !sp.Sponsor.IsDeleted)
                             || (sp.CompanyId != null
                                 && sp.Company != null
                                 && sp.Company.IsActive)))
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

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        // The current plan is the most recently started, still-active planned delivery
        // for the manager's chapter whose collection window has already begun.
        return await db.PlannedDeliveries
            .Where(pd => pd.IsActive
                         && pd.ChapterId == chapterId
                         && pd.StartsOn <= utcNow)
            .OrderByDescending(pd => pd.StartsOn)
            .Select(pd => new CurrentPlanDataModel
            {
                PlanId = pd.Id,
                StartsOn = pd.StartsOn,
            })
            .FirstOrDefaultAsync();
    }
}