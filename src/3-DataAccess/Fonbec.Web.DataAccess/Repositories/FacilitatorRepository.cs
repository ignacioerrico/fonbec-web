using Fonbec.Web.DataAccess.DataModels.Facilitators;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Repositories;

public interface IFacilitatorRepository
{
    Task<List<FacilitatorStudentsDataModel>> GetActiveSponsoredStudentsAsync(int facilitatorId);

    Task<CurrentPlanDataModel?> GetCurrentPlanForFacilitatorAsync(int facilitatorId);
}

public class FacilitatorRepository(IDbContextFactory<FonbecWebDbContext> dbContext) : IFacilitatorRepository
{
    public async Task<List<FacilitatorStudentsDataModel>> GetActiveSponsoredStudentsAsync(int facilitatorId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var utcNow = DateTime.UtcNow;

        // NOTE: the sponsorship predicate below is intentionally duplicated between the outer
        // student inclusion filter and the inner "Sponsors" projection so that a listed student
        // only ever shows the same active sponsors/companies that made them eligible. The
        // predicate is inlined (not a shared method) because EF Core cannot translate a custom
        // method call into SQL. Keep both copies in sync.
        var students = await db.Students
            .Include(s => s.Facilitator)
            .Include(s => s.CreatedBy)
            .Include(s => s.LastUpdatedBy)
            .Include(s => s.DisabledBy)
            .Include(s => s.ReenabledBy)
            .Where(s => s.FacilitatorId == facilitatorId
                        && s.IsActive
                        && !s.IsDeleted
                        && s.Sponsorships.Any(sp =>
                            sp.IsActive
                            && sp.StartDate <= utcNow
                            && (sp.EndDate == null || sp.EndDate >= utcNow)
                            && (
                                (sp.SponsorId != null
                                 && sp.Sponsor != null
                                 && sp.Sponsor.IsActive
                                 && !sp.Sponsor.IsDeleted)
                                || (sp.CompanyId != null
                                    && sp.Company != null
                                    && sp.Company.IsActive))))
            .Select(s => new FacilitatorStudentsDataModel(s)
            {
                StudentId = s.Id,
                StudentFirstName = s.FirstName,
                StudentLastName = s.LastName,
                StudentNickName = s.NickName,
                EducationLevel = s.CurrentEducationLevel,
                Sponsors = s.Sponsorships
                    .Where(sp =>
                        sp.IsActive
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
                    .Select(sp => new DashboardSponsorDataModel
                    {
                        SponsorshipId = sp.Id,
                        SponsorId = sp.SponsorId,
                        CompanyId = sp.CompanyId,
                        RecipientName = sp.CompanyId != null && sp.Company != null
                            ? sp.Company.Name
                            : sp.Sponsor != null
                                ? sp.Sponsor.FirstName + " " + sp.Sponsor.LastName
                                : string.Empty,
                        IsCompany = sp.CompanyId != null,
                    })
                    .ToList(),
            })
            .OrderBy(s => s.StudentFirstName)
            .ThenBy(s => s.StudentLastName)
            .ToListAsync();

        return students;
    }

    public async Task<CurrentPlanDataModel?> GetCurrentPlanForFacilitatorAsync(int facilitatorId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var utcNow = DateTime.UtcNow;

        var chapterId = await db.Users
            .Where(u => u.Id == facilitatorId)
            .Select(u => u.ChapterId)
            .FirstOrDefaultAsync();

        // The current plan is the most recently started, still-active planned delivery
        // for the facilitator's chapter whose collection window has already begun.
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