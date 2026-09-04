using Fonbec.Web.DataAccess.DataModels.Facilitators;
using Fonbec.Web.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Repositories;

public interface IFacilitatorRepository
{
    Task<List<FacilitatorStudentsDataModel>> GetActiveSponsoredStudentsAsync(int facilitatorId);

    Task<CurrentPlanDataModel?> GetCurrentPlanForFacilitatorAsync(int facilitatorId);

    Task<FacilitatorUploadContextDataModel?> GetUploadContextAsync(
        int studentId, int? planId, int? sponsorId, int? companyId);

    Task<List<SponsorLetterStatusDataModel>> GetCurrentLetterStatusesAsync(int planId, List<int> studentIds);

    Task<List<FacilitatorReportsDataModel>> GetLatestReportCardsAsync(List<int> studentIds, int count);
}

public class FacilitatorRepository(
    IDbContextFactory<FonbecWebDbContext> dbContext,
    TimeProvider timeProvider) : IFacilitatorRepository
{
    public async Task<List<FacilitatorStudentsDataModel>> GetActiveSponsoredStudentsAsync(int facilitatorId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        // NOTE: the sponsorship predicate below is intentionally duplicated between the outer
        // student inclusion filter and the inner "Sponsors" projection so that a listed student
        // only ever shows the same active sponsors/companies that made them eligible. The
        // predicate is inlined (not a shared method) because EF Core cannot translate a custom
        // method call into SQL. Keep both copies in sync.
        var students = await db.Students
            .AsNoTracking()
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

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var chapterId = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == facilitatorId)
            .Select(u => u.ChapterId)
            .FirstOrDefaultAsync();

        // The current plan is the most recently started, still-active planned delivery
        // for the facilitator's chapter whose collection window has already begun.
        return await db.PlannedDeliveries
            .AsNoTracking()
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

    public async Task<FacilitatorUploadContextDataModel?> GetUploadContextAsync(
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
                s.FacilitatorId,
                s.IsActive,
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

        return new FacilitatorUploadContextDataModel
        {
            StudentId = student.Id,
            StudentFirstName = student.FirstName,
            StudentLastName = student.LastName,
            ChapterId = student.ChapterId,
            FacilitatorId = student.FacilitatorId,
            IsActive = student.IsActive,
            SecondarySchoolStartYear = student.SecondarySchoolStartYear,
            UniversityStartYear = student.UniversityStartYear,
            PlanStartsOn = planStartsOn,
            SponsorFirstName = sponsorFirstName,
            SponsorLastName = sponsorLastName,
            CompanyName = companyName,
        };
    }

    public async Task<List<SponsorLetterStatusDataModel>> GetCurrentLetterStatusesAsync(int planId, List<int> studentIds)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var letters = await db.Set<Letter>()
            .AsNoTracking()
            .Where(l => l.PlanId == planId && studentIds.Contains(l.StudentId))
            .OrderByDescending(l => l.UploadedOn)
            .ThenByDescending(l => l.DocumentId)
            .Select(l => new SponsorLetterStatusDataModel
            {
                StudentId = l.StudentId,
                SponsorId = l.SponsorId,
                CompanyId = l.CompanyId,
                Status = l.Status,
                RejectionReason = l.RejectedReason != null ? l.RejectedReason.Description : l.RejectionNotes,
            })
            .ToListAsync();

        // A slot (student + sponsor/company) can have several letters across resubmissions;
        // only the most recent one (already sorted above) is the "current" one for that slot.
        return letters
            .GroupBy(l => (l.StudentId, l.SponsorId, l.CompanyId))
            .Select(g => g.First())
            .ToList();
    }

    public async Task<List<FacilitatorReportsDataModel>> GetLatestReportCardsAsync(List<int> studentIds, int count)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var reportCards =  await db.Set<ReportCard>()
            .AsNoTracking()
            .Where(r => studentIds.Contains(r.StudentId))
            .Select(r => new FacilitatorReportsDataModel
            {
                ReportCardId = r.DocumentId,
                StudentId = r.StudentId,
                Period = r.Period,
                Description = r.Description,
                Status = r.Status,
                RejectionReason = r.RejectionNotes
            })
            .OrderByDescending(r => r.Period)
            .ThenByDescending(r => r.ReportCardId)
            .ToListAsync();

        return reportCards
        .GroupBy(r => r.StudentId)
        .SelectMany(g => g.Take(count))
        .ToList();
    }
}