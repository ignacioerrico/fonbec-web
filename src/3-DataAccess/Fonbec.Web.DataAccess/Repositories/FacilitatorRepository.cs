using Fonbec.Web.DataAccess.DataModels.Facilitators;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Queries;
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

    Task<Dictionary<int, int>> GetOtherDocumentCountsAsync(List<int> studentIds);

    /// <summary>
    /// Other documents for one of the facilitator's active students, newest first.
    /// Null when the student is missing, inactive, deleted, or assigned to someone else.
    /// </summary>
    Task<FacilitatorOtherDocumentsHistoryDataModel?> GetOtherDocumentsHistoryAsync(int facilitatorId, int studentId);
}

public class FacilitatorRepository(
    IDbContextFactory<FonbecWebDbContext> dbContext,
    TimeProvider timeProvider) : IFacilitatorRepository
{
    public async Task<List<FacilitatorStudentsDataModel>> GetActiveSponsoredStudentsAsync(int facilitatorId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var chapterId = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == facilitatorId)
            .Select(u => u.ChapterId)
            .FirstOrDefaultAsync();

        // With no open plan, list whoever is sponsored today.
        var asOf = await CampaignQueries.SelectStartsOnAsync(
                       CampaignQueries.OpenForChapter(db.PlannedDeliveries.AsNoTracking(), chapterId))
                   ?? utcNow;

        var students = await db.Students
            .AsNoTracking()
            .Include(s => s.Facilitator)
            .Include(s => s.CreatedBy)
            .Include(s => s.LastUpdatedBy)
            .Include(s => s.DisabledBy)
            .Include(s => s.ReenabledBy)
            .Where(s => s.FacilitatorId == facilitatorId)
            .WhereCovered(CampaignQueries.Covers(asOf))
            .Select(s => new FacilitatorStudentsDataModel(s)
            {
                StudentId = s.Id,
                StudentFirstName = s.FirstName,
                StudentLastName = s.LastName,
                StudentNickName = s.NickName,
                EducationLevel = s.CurrentEducationLevel,
                Sponsors = s.Sponsorships
                    .AsQueryable()
                    .Where(CampaignQueries.Covers(asOf))
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

        var chapterId = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == facilitatorId)
            .Select(u => u.ChapterId)
            .FirstOrDefaultAsync();

        return await CampaignQueries.SelectCurrentAsync(
            CampaignQueries.OpenForChapter(db.PlannedDeliveries.AsNoTracking(), chapterId));
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
                RejectionReason = l.RejectedReason != null
                    ? l.RejectionNotes != null && l.RejectionNotes != string.Empty
                        ? l.RejectedReason.Description + ": " + l.RejectionNotes
                        : l.RejectedReason.Description
                    : l.RejectionNotes,
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

        var reportCards = db.Set<ReportCard>().AsNoTracking();

        // Top-N-per-student expressed as a correlated "rank" filter: a report card is kept only
        // when fewer than `count` of the same student's report cards are more recent. This
        // translates to a single SQL statement (a correlated COUNT subquery) so the database
        // returns at most `count` rows per student, instead of loading every report card and
        // trimming in memory.
        return await reportCards
            .Where(r => studentIds.Contains(r.StudentId)
                        && reportCards.Count(newer =>
                            newer.StudentId == r.StudentId
                            && (newer.Period > r.Period
                                || (newer.Period == r.Period && newer.DocumentId > r.DocumentId))) < count)
            .OrderByDescending(r => r.Period)
            .ThenByDescending(r => r.DocumentId)
            .Select(r => new FacilitatorReportsDataModel
            {
                ReportCardId = r.DocumentId,
                StudentId = r.StudentId,
                Period = r.Period,
                Description = r.Description,
                Status = r.Status,
                RejectionReason = r.RejectedReason != null ? r.RejectedReason.Description : r.RejectionNotes
            })
            .ToListAsync();
    }

    public async Task<Dictionary<int, int>> GetOtherDocumentCountsAsync(List<int> studentIds)
    {
        if (studentIds.Count == 0)
        {
            return [];
        }

        await using var db = await dbContext.CreateDbContextAsync();

        var counts = await db.Set<OtherDocument>()
            .AsNoTracking()
            .Where(d => studentIds.Contains(d.StudentId))
            .GroupBy(d => d.StudentId)
            .Select(g => new { StudentId = g.Key, Count = g.Count() })
            .ToListAsync();

        return counts.ToDictionary(c => c.StudentId, c => c.Count);
    }

    public async Task<FacilitatorOtherDocumentsHistoryDataModel?> GetOtherDocumentsHistoryAsync(
        int facilitatorId, int studentId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var studentName = await db.Students
            .AsNoTracking()
            .Where(s => s.Id == studentId
                        && s.FacilitatorId == facilitatorId
                        && s.IsActive
                        && !s.IsDeleted)
            .Select(s => s.FirstName + " " + s.LastName)
            .FirstOrDefaultAsync();

        if (studentName is null)
        {
            return null;
        }

        var items = await db.Set<OtherDocument>()
            .AsNoTracking()
            .Where(d => d.StudentId == studentId)
            .OrderByDescending(d => d.UploadedOn)
            .ThenByDescending(d => d.DocumentId)
            .Select(d => new FacilitatorOtherDocumentItemDataModel
            {
                DocumentId = d.DocumentId,
                UploadedOn = d.UploadedOn,
                Description = d.Description,
                FileKind = d.FileKind,
                Status = d.Status,
                RejectionReason = d.RejectedReason != null
                    ? d.RejectionNotes != null && d.RejectionNotes != string.Empty
                        ? d.RejectedReason.Description + ": " + d.RejectionNotes
                        : d.RejectedReason.Description
                    : d.RejectionNotes,
                TextContent = d.TextContent,
                YouTubeVideoId = d.YouTubeVideoId,
                PageCount = d.Pages.Count,
            })
            .ToListAsync();

        return new FacilitatorOtherDocumentsHistoryDataModel
        {
            StudentId = studentId,
            StudentName = studentName,
            Items = items,
        };
    }
}