using Fonbec.Web.DataAccess.DataModels.Sponsorships;
using Fonbec.Web.DataAccess.DataModels.Sponsorships.Input;
using Fonbec.Web.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace Fonbec.Web.DataAccess.Repositories;

public interface ISponsorshipRepository
{
    Task<AllSponsorshipsDataModel> GetAllSponsorshipsAsync(int studentId);
    Task<SponsorshipPeriodMatch> GetSponsorshipPeriodMatchAsync(
        CreateSponsorshipInputDataModel inputDataModel);
    Task<SponsorshipPeriodMatch> GetSponsorshipPeriodMatchForUpdateAsync(
        UpdateSponsorshipInputDataModel inputDataModel);
    Task<CreateSponsorshipRepositoryResult> CreateSponsorshipAsync(
        CreateSponsorshipInputDataModel inputDataModel);
    Task<UpdateSponsorshipRepositoryResult> UpdateSponsorshipAsync(
        UpdateSponsorshipInputDataModel inputDataModel);
}

public class SponsorshipRepository(IDbContextFactory<FonbecWebDbContext> dbContext) : ISponsorshipRepository
{
    public async Task<AllSponsorshipsDataModel> GetAllSponsorshipsAsync(int studentId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var lockedPlanMonths = await GetLockedPlanMonthsAsync(db, studentId);

        // Read the name from the student, not from a sponsorship, so it is also known
        // for a student who has none yet.
        var student = await db.Students
            .AsNoTracking()
            .Where(s => s.Id == studentId)
            .Select(s => new { s.FirstName, s.LastName })
            .FirstOrDefaultAsync();

        var allSponsorshipsForStudent = await db.Sponsorships
            .AsNoTracking()
            .Include(s => s.Sponsor!)
                .ThenInclude(sp => sp.Company)
            .Include(s => s.Company)
            .Include(s => s.CreatedBy)
            .Include(s => s.LastUpdatedBy)
            .Include(s => s.DisabledBy)
            .Include(s => s.ReenabledBy)
            .Where(s => s.StudentId == studentId && s.IsActive)
            .OrderBy(s => s.StartDate)
            .ThenBy(s => s.EndDate)
            .ToListAsync();

        var allSponsorships = new AllSponsorshipsDataModel
        {
            StudentFullName = student is null
                ? null
                : $"{student.FirstName} {student.LastName}",
            Sponsorships = allSponsorshipsForStudent
                .Select(s => new AllSponsorshipsSponsorshipsDataModel(s)
                {
                    SponsorshipId = s.Id,
                    Sponsor = s.Sponsor,
                    Company = s.Company,
                    SponsorshipStartDate = s.StartDate,
                    SponsorshipEndDate = s.EndDate,
                    LockedPlanStartsOn = UniquelyCoveredLockedMonths(
                        s,
                        allSponsorshipsForStudent,
                        lockedPlanMonths),
                })
                .ToList(),
        };
        return allSponsorships;
    }

    public async Task<SponsorshipPeriodMatch> GetSponsorshipPeriodMatchAsync(
        CreateSponsorshipInputDataModel inputDataModel)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var (startDate, endDate) = NormalizePeriod(inputDataModel);
        var existingSponsorships = await GetMatchingSponsorshipsAsync(db, inputDataModel);
        return GetPeriodMatch(existingSponsorships, startDate, endDate);
    }

    public async Task<SponsorshipPeriodMatch> GetSponsorshipPeriodMatchForUpdateAsync(
        UpdateSponsorshipInputDataModel inputDataModel)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var sponsorship = await db.Sponsorships
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == inputDataModel.SponsorshipId && s.IsActive);
        if (sponsorship is null)
        {
            return SponsorshipPeriodMatch.None;
        }

        var (startDate, endDate) = NormalizePeriod(
            inputDataModel.SponsorshipStartDate,
            inputDataModel.SponsorshipEndDate);
        var existingSponsorships = await GetMatchingSponsorshipsAsync(
            db,
            sponsorship.StudentId,
            sponsorship.SponsorId,
            sponsorship.CompanyId,
            excludeSponsorshipId: sponsorship.Id);
        return GetPeriodMatch(existingSponsorships, startDate, endDate);
    }

    public async Task<CreateSponsorshipRepositoryResult> CreateSponsorshipAsync(
        CreateSponsorshipInputDataModel inputDataModel)
    {
        await using var db = await dbContext.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var (startDate, endDate) = NormalizePeriod(inputDataModel);
        if (endDate < startDate)
        {
            return new CreateSponsorshipRepositoryResult();
        }

        var existingSponsorships = await GetMatchingSponsorshipsAsync(db, inputDataModel);
        var periodMatch = GetPeriodMatch(existingSponsorships, startDate, endDate);
        if (periodMatch == SponsorshipPeriodMatch.Overlap)
        {
            return new CreateSponsorshipRepositoryResult(PeriodMatch: periodMatch);
        }

        var periodsBefore = existingSponsorships
            .Select(s => (s.StartDate, s.EndDate))
            .ToList();
        var periodsAfter = new List<(DateTime StartDate, DateTime? EndDate)>(periodsBefore);
        if (periodMatch == SponsorshipPeriodMatch.Adjacent)
        {
            var adjacentSponsorships = existingSponsorships
                .Where(s => IsAdjacent(s, startDate, endDate))
                .ToList();
            foreach (var adjacent in adjacentSponsorships)
            {
                periodsAfter.RemoveAll(p => p.StartDate == adjacent.StartDate && p.EndDate == adjacent.EndDate);
            }

            var mergedStart = adjacentSponsorships
                .Select(s => s.StartDate)
                .Append(startDate)
                .Min();
            DateTime? mergedEnd =
                adjacentSponsorships.Any(s => s.EndDate is null) || endDate is null
                    ? null
                    : adjacentSponsorships
                        .Select(s => s.EndDate!.Value)
                        .Append(endDate.Value)
                        .Max();
            periodsAfter.Add((mergedStart, mergedEnd));
        }
        else
        {
            periodsAfter.Add((startDate, endDate));
        }

        var completedPlanStartsOn = await GetNewlyCoveredCompletedPlanStartsOnAsync(
            db,
            inputDataModel.StudentId,
            periodsBefore,
            periodsAfter);
        if (completedPlanStartsOn.Count > 0)
        {
            return new CreateSponsorshipRepositoryResult(CompletedPlanStartsOn: completedPlanStartsOn);
        }

        if (periodMatch == SponsorshipPeriodMatch.Adjacent)
        {
            var adjacentSponsorships = existingSponsorships
                .Where(s => IsAdjacent(s, startDate, endDate))
                .OrderBy(s => s.StartDate)
                .ToList();
            var sponsorshipToExtend = adjacentSponsorships[0];

            sponsorshipToExtend.StartDate = adjacentSponsorships
                .Select(s => s.StartDate)
                .Append(startDate)
                .Min();
            sponsorshipToExtend.EndDate =
                adjacentSponsorships.Any(s => s.EndDate is null) || endDate is null
                    ? null
                    : adjacentSponsorships
                        .Select(s => s.EndDate!.Value)
                        .Append(endDate.Value)
                        .Max();
            sponsorshipToExtend.LastUpdatedById = inputDataModel.CreatedById;

            if (!string.IsNullOrWhiteSpace(inputDataModel.SponsorshipNotes))
            {
                sponsorshipToExtend.Notes = inputDataModel.SponsorshipNotes;
            }

            foreach (var duplicate in adjacentSponsorships.Skip(1))
            {
                duplicate.DisabledById = inputDataModel.CreatedById;
            }

            var extendedRows = await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return new CreateSponsorshipRepositoryResult(extendedRows, periodMatch);
        }

        var sponsorship = new Sponsorship
        {
            StudentId = inputDataModel.StudentId,
            SponsorId = inputDataModel.SponsorId,
            CompanyId = inputDataModel.CompanyId,
            StartDate = startDate,
            EndDate = endDate,
            Notes = inputDataModel.SponsorshipNotes,
            CreatedById = inputDataModel.CreatedById,
        };
        db.Sponsorships.Add(sponsorship);
        var createdRows = await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return new CreateSponsorshipRepositoryResult(createdRows);
    }

    public async Task<UpdateSponsorshipRepositoryResult> UpdateSponsorshipAsync(
        UpdateSponsorshipInputDataModel inputDataModel)
    {
        await using var db = await dbContext.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        var sponsorship = await db.Sponsorships
            .FirstOrDefaultAsync(s => s.Id == inputDataModel.SponsorshipId && s.IsActive);
        if (sponsorship is null)
        {
            return new UpdateSponsorshipRepositoryResult(Outcome: UpdateSponsorshipOutcome.NotFound);
        }

        var (startDate, endDate) = NormalizePeriod(
            inputDataModel.SponsorshipStartDate,
            inputDataModel.SponsorshipEndDate);
        if (endDate < startDate)
        {
            return new UpdateSponsorshipRepositoryResult();
        }

        var others = await GetMatchingSponsorshipsAsync(
            db,
            sponsorship.StudentId,
            sponsorship.SponsorId,
            sponsorship.CompanyId,
            excludeSponsorshipId: sponsorship.Id);
        var periodMatch = GetPeriodMatch(others, startDate, endDate);
        if (periodMatch == SponsorshipPeriodMatch.Overlap)
        {
            return new UpdateSponsorshipRepositoryResult(
                Outcome: UpdateSponsorshipOutcome.Overlap,
                PeriodMatch: periodMatch);
        }

        var remainingOthers = others;
        var mergedSponsorshipIds = new List<int>();
        if (periodMatch == SponsorshipPeriodMatch.Adjacent)
        {
            var adjacentSponsorships = others
                .Where(s => IsAdjacent(s, startDate, endDate))
                .OrderBy(s => s.StartDate)
                .ToList();
            startDate = adjacentSponsorships
                .Select(s => s.StartDate)
                .Append(startDate)
                .Min();
            endDate =
                adjacentSponsorships.Any(s => s.EndDate is null) || endDate is null
                    ? null
                    : adjacentSponsorships
                        .Select(s => s.EndDate!.Value)
                        .Append(endDate.Value)
                        .Max();

            mergedSponsorshipIds = adjacentSponsorships.Select(s => s.Id).ToList();
            remainingOthers = others.Except(adjacentSponsorships).ToList();
        }

        var periodsBefore = others
            .Select(s => (s.StartDate, s.EndDate))
            .Append((sponsorship.StartDate, sponsorship.EndDate))
            .ToList();
        var periodsAfter = remainingOthers
            .Select(s => (s.StartDate, s.EndDate))
            .Append((startDate, endDate))
            .ToList();
        var completedPlanStartsOn = await GetNewlyCoveredCompletedPlanStartsOnAsync(
            db,
            sponsorship.StudentId,
            periodsBefore,
            periodsAfter);
        if (completedPlanStartsOn.Count > 0)
        {
            return new UpdateSponsorshipRepositoryResult(
                Outcome: UpdateSponsorshipOutcome.AddsSlotToCompletedPlan,
                CompletedPlanStartsOn: completedPlanStartsOn);
        }

        if (periodMatch == SponsorshipPeriodMatch.Adjacent)
        {
            foreach (var duplicate in others.Where(s => mergedSponsorshipIds.Contains(s.Id)))
            {
                duplicate.DisabledById = inputDataModel.UpdatedById;
            }
        }

        var remainingPeriods = remainingOthers
            .Select(s => (s.StartDate, s.EndDate))
            .Append((startDate, endDate))
            .ToList();
        var lockedPlanMonths = await GetLockedPlanMonthsAsync(
            db,
            sponsorship.StudentId,
            sponsorship.SponsorId,
            sponsorship.CompanyId);
        var uncovered = lockedPlanMonths
            .Where(startsOn => !remainingPeriods.Any(period =>
                Covers(period.Item1, period.Item2, startsOn)))
            .ToList();
        if (uncovered.Count > 0)
        {
            return new UpdateSponsorshipRepositoryResult(
                Outcome: UpdateSponsorshipOutcome.UncoversLockedPlan,
                UncoveredPlanStartsOn: uncovered);
        }

        var exemptionsToRevoke = await GetExemptionsThatLoseAllSlotsAsync(
            db,
            sponsorship,
            startDate,
            endDate,
            mergedSponsorshipIds);
        if (exemptionsToRevoke.Count > 0 && !inputDataModel.ConfirmExemptionRevocation)
        {
            return new UpdateSponsorshipRepositoryResult(
                Outcome: UpdateSponsorshipOutcome.RequiresExemptionRevocation,
                ExemptPlanStartsOn: exemptionsToRevoke
                    .Select(e => e.PlannedDelivery.StartsOn)
                    .OrderBy(d => d)
                    .ToList());
        }

        foreach (var exemption in exemptionsToRevoke)
        {
            exemption.IsRevoked = true;
            exemption.RevokedByFonbecUserId = inputDataModel.UpdatedById;
            exemption.RevokedOnUtc = DateTime.UtcNow;
        }

        sponsorship.StartDate = startDate;
        sponsorship.EndDate = endDate;
        sponsorship.Notes = inputDataModel.SponsorshipNotes;
        sponsorship.LastUpdatedById = inputDataModel.UpdatedById;

        var affectedRows = await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return new UpdateSponsorshipRepositoryResult(affectedRows, PeriodMatch: periodMatch);
    }

    private static async Task<List<LetterExemption>> GetExemptionsThatLoseAllSlotsAsync(
        FonbecWebDbContext db,
        Sponsorship editedSponsorship,
        DateTime newStartDate,
        DateTime? newEndDate,
        IReadOnlyCollection<int> mergedSponsorshipIds)
    {
        var activeExemptions = await db.LetterExemptions
            .Include(e => e.PlannedDelivery)
            .Where(e => e.StudentId == editedSponsorship.StudentId
                        && !e.IsRevoked
                        && editedSponsorship.StartDate <= e.PlannedDelivery.StartsOn
                        && (editedSponsorship.EndDate == null
                            || editedSponsorship.EndDate >= e.PlannedDelivery.StartsOn))
            .ToListAsync();
        if (activeExemptions.Count == 0)
        {
            return [];
        }

        var otherPeriods = await db.Sponsorships
            .AsNoTracking()
            .Where(s => s.StudentId == editedSponsorship.StudentId
                        && s.IsActive
                        && s.Id != editedSponsorship.Id
                        && !mergedSponsorshipIds.Contains(s.Id)
                        && ((s.SponsorId != null
                             && s.Sponsor != null
                             && s.Sponsor.IsActive
                             && !s.Sponsor.IsDeleted)
                            || (s.CompanyId != null
                                && s.Company != null
                                && s.Company.IsActive)))
            .Select(s => new { s.StartDate, s.EndDate })
            .ToListAsync();

        var editedRecipientIsEligible = editedSponsorship.SponsorId is int sponsorId
            ? await db.Sponsors.AnyAsync(s => s.Id == sponsorId && s.IsActive && !s.IsDeleted)
            : editedSponsorship.CompanyId is int companyId
              && await db.Companies.AnyAsync(c => c.Id == companyId && c.IsActive);

        var resultingPeriods = otherPeriods
            .Select(p => (p.StartDate, p.EndDate))
            .ToList();
        if (editedRecipientIsEligible)
        {
            resultingPeriods.Add((newStartDate, newEndDate));
        }

        return activeExemptions
            .Where(e => !resultingPeriods.Any(period =>
                Covers(period.StartDate, period.EndDate, e.PlannedDelivery.StartsOn)))
            .ToList();
    }

    private static async Task<List<DateTime>> GetNewlyCoveredCompletedPlanStartsOnAsync(
        FonbecWebDbContext db,
        int studentId,
        IReadOnlyCollection<(DateTime StartDate, DateTime? EndDate)> periodsBefore,
        IReadOnlyCollection<(DateTime StartDate, DateTime? EndDate)> periodsAfter)
    {
        var chapterId = await db.Students
            .AsNoTracking()
            .Where(s => s.Id == studentId)
            .Select(s => (int?)s.ChapterId)
            .FirstOrDefaultAsync();
        if (chapterId is null or 0)
        {
            return [];
        }

        var completedStartsOn = await db.PlannedDeliveries
            .AsNoTracking()
            .Where(p => p.IsActive && p.ChapterId == chapterId && p.Completed)
            .Select(p => p.StartsOn)
            .ToListAsync();

        return completedStartsOn
            .Where(startsOn =>
                periodsAfter.Any(period => Covers(period.StartDate, period.EndDate, startsOn))
                && !periodsBefore.Any(period => Covers(period.StartDate, period.EndDate, startsOn)))
            .OrderBy(startsOn => startsOn)
            .ToList();
    }

    private static Task<List<Sponsorship>> GetMatchingSponsorshipsAsync(
        FonbecWebDbContext db,
        CreateSponsorshipInputDataModel inputDataModel) =>
        GetMatchingSponsorshipsAsync(
            db,
            inputDataModel.StudentId,
            inputDataModel.SponsorId,
            inputDataModel.CompanyId);

    private static Task<List<Sponsorship>> GetMatchingSponsorshipsAsync(
        FonbecWebDbContext db,
        int studentId,
        int? sponsorId,
        int? companyId,
        int? excludeSponsorshipId = null) =>
        db.Sponsorships
            .Where(s => s.IsActive
                        && s.StudentId == studentId
                        && s.SponsorId == sponsorId
                        && s.CompanyId == companyId
                        && (excludeSponsorshipId == null || s.Id != excludeSponsorshipId))
            .OrderBy(s => s.StartDate)
            .ToListAsync();

    private static SponsorshipPeriodMatch GetPeriodMatch(
        IEnumerable<Sponsorship> existingSponsorships,
        DateTime startDate,
        DateTime? endDate)
    {
        var sponsorships = existingSponsorships.ToList();
        if (sponsorships.Any(s => Overlaps(s, startDate, endDate)))
        {
            return SponsorshipPeriodMatch.Overlap;
        }

        return sponsorships.Any(s => IsAdjacent(s, startDate, endDate))
            ? SponsorshipPeriodMatch.Adjacent
            : SponsorshipPeriodMatch.None;
    }

    private static bool Overlaps(Sponsorship existing, DateTime startDate, DateTime? endDate) =>
        existing.StartDate <= (endDate ?? DateTime.MaxValue)
        && startDate <= (existing.EndDate ?? DateTime.MaxValue);

    private static bool IsAdjacent(Sponsorship existing, DateTime startDate, DateTime? endDate) =>
        (existing.EndDate is DateTime existingEndDate
         && existingEndDate.AddDays(1) == startDate)
        || (endDate is DateTime newEndDate
            && newEndDate.AddDays(1) == existing.StartDate);

    private static (DateTime StartDate, DateTime? EndDate) NormalizePeriod(
        CreateSponsorshipInputDataModel inputDataModel) =>
        NormalizePeriod(inputDataModel.SponsorshipStartDate, inputDataModel.SponsorshipEndDate);

    private static (DateTime StartDate, DateTime? EndDate) NormalizePeriod(
        DateTime start,
        DateTime? end)
    {
        var startDate = new DateTime(start.Year, start.Month, 1);
        DateTime? endDate = end is DateTime requestedEndDate
            ? new DateTime(
                requestedEndDate.Year,
                requestedEndDate.Month,
                DateTime.DaysInMonth(requestedEndDate.Year, requestedEndDate.Month))
            : null;

        return (startDate, endDate);
    }

    private static bool Covers(DateTime startDate, DateTime? endDate, DateTime planStartsOn) =>
        startDate <= planStartsOn && (endDate is null || endDate >= planStartsOn);

    private static async Task<List<LockedPlanMonth>> GetLockedPlanMonthsAsync(
        FonbecWebDbContext db,
        int studentId)
    {
        var rows = await db.Set<Letter>()
            .AsNoTracking()
            .Where(l => l.StudentId == studentId)
            .Select(l => new { l.SponsorId, l.CompanyId, l.Plan.StartsOn })
            .Distinct()
            .ToListAsync();

        return rows
            .Select(r => new LockedPlanMonth(r.SponsorId, r.CompanyId, r.StartsOn))
            .ToList();
    }

    private static async Task<List<DateTime>> GetLockedPlanMonthsAsync(
        FonbecWebDbContext db,
        int studentId,
        int? sponsorId,
        int? companyId) =>
        (await GetLockedPlanMonthsAsync(db, studentId))
            .Where(m => m.SponsorId == sponsorId && m.CompanyId == companyId)
            .Select(m => m.StartsOn)
            .Distinct()
            .ToList();

    private static List<DateTime> UniquelyCoveredLockedMonths(
        Sponsorship sponsorship,
        IReadOnlyCollection<Sponsorship> allForStudent,
        IReadOnlyCollection<LockedPlanMonth> lockedPlanMonths)
    {
        var pairLocked = lockedPlanMonths
            .Where(m => m.SponsorId == sponsorship.SponsorId && m.CompanyId == sponsorship.CompanyId)
            .Select(m => m.StartsOn)
            .Distinct();

        return pairLocked
            .Where(startsOn =>
                Covers(sponsorship.StartDate, sponsorship.EndDate, startsOn)
                && !allForStudent.Any(other =>
                    other.Id != sponsorship.Id
                    && other.SponsorId == sponsorship.SponsorId
                    && other.CompanyId == sponsorship.CompanyId
                    && Covers(other.StartDate, other.EndDate, startsOn)))
            .OrderBy(startsOn => startsOn)
            .ToList();
    }

    private readonly record struct LockedPlanMonth(int? SponsorId, int? CompanyId, DateTime StartsOn);
}
