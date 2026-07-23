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

        DateTime? planStartsOn = null;
        if (planId.HasValue)
        {
            planStartsOn = await db.PlannedDeliveries
                .AsNoTracking()
                .Where(p => p.Id == planId.Value)
                .Select(p => (DateTime?)p.StartsOn)
                .FirstOrDefaultAsync();
        }

        string? sponsorFirstName = null;
        string? sponsorLastName = null;
        if (sponsorId.HasValue)
        {
            var sponsor = await db.Sponsors
                .AsNoTracking()
                .Where(s => s.Id == sponsorId.Value && !s.IsDeleted)
                .Select(s => new { s.FirstName, s.LastName })
                .FirstOrDefaultAsync();
            sponsorFirstName = sponsor?.FirstName;
            sponsorLastName = sponsor?.LastName;
        }

        string? companyName = null;
        if (companyId.HasValue)
        {
            companyName = await db.Companies
                .AsNoTracking()
                .Where(c => c.Id == companyId.Value)
                .Select(c => c.Name)
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
