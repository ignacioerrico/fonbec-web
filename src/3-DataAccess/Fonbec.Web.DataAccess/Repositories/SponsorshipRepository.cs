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
    Task<CreateSponsorshipRepositoryResult> CreateSponsorshipAsync(
        CreateSponsorshipInputDataModel inputDataModel);
}

public class SponsorshipRepository(IDbContextFactory<FonbecWebDbContext> dbContext) : ISponsorshipRepository
{
    public async Task<AllSponsorshipsDataModel> GetAllSponsorshipsAsync(int studentId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var allSponsorshipsForStudent = await db.Sponsorships
            .AsNoTracking()
            .Include(s => s.Student)
            .Include(s => s.Sponsor!)
                .ThenInclude(sp => sp.Company)
            .Include(s => s.Company)
            .Include(s => s.CreatedBy)
            .Include(s => s.LastUpdatedBy)
            .Include(s => s.DisabledBy)
            .Include(s => s.ReenabledBy)
            .Where(s => s.StudentId == studentId && s.IsActive)
            .ToListAsync();

        var allSponsorships = new AllSponsorshipsDataModel
        {
            StudentFullName = allSponsorshipsForStudent
                .FirstOrDefault()?
                .Student
                .FullName(),
            Sponsorships = allSponsorshipsForStudent
                .Select(s => new AllSponsorshipsSponsorshipsDataModel(s)
                {
                    SponsorshipId = s.Id,
                    Sponsor = s.Sponsor,
                    Company = s.Company,
                    SponsorshipStartDate = s.StartDate,
                    SponsorshipEndDate = s.EndDate,
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

    private static Task<List<Sponsorship>> GetMatchingSponsorshipsAsync(
        FonbecWebDbContext db,
        CreateSponsorshipInputDataModel inputDataModel) =>
        db.Sponsorships
            .Where(s => s.IsActive
                        && s.StudentId == inputDataModel.StudentId
                        && s.SponsorId == inputDataModel.SponsorId
                        && s.CompanyId == inputDataModel.CompanyId)
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
        CreateSponsorshipInputDataModel inputDataModel)
    {
        var startDate = new DateTime(
            inputDataModel.SponsorshipStartDate.Year,
            inputDataModel.SponsorshipStartDate.Month,
            1);
        DateTime? endDate = inputDataModel.SponsorshipEndDate is DateTime requestedEndDate
            ? new DateTime(
                requestedEndDate.Year,
                requestedEndDate.Month,
                DateTime.DaysInMonth(requestedEndDate.Year, requestedEndDate.Month))
            : null;

        return (startDate, endDate);
    }
}