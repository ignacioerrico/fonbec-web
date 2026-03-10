using Fonbec.Web.DataAccess.DataModels.Sponsorships;
using Fonbec.Web.DataAccess.DataModels.Sponsorships.Input;
using Fonbec.Web.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Repositories;

public interface ISponsorshipRepository
{
    /// <param name="studentId">Use <c>null</c> to get all sponsorships for all students.</param>
    /// <returns>A list of <see cref="AllSponsorshipsDataModel"/></returns>
    Task<List<AllSponsorshipsDataModel>> GetAllSponsorshipsAsync(int studentId);
    Task<int> CreateSponsorshipAsync(CreateSponsorshipInputDataModel inputDataModel);
}

public class SponsorshipRepository(IDbContextFactory<FonbecWebDbContext> dbContext) : ISponsorshipRepository
{
    public async Task<int> CreateSponsorshipAsync(CreateSponsorshipInputDataModel inputDataModel)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var sponsorship = new Sponsorship
        {
            StudentId = inputDataModel.StudentId,
            SponsorId = inputDataModel.SponsorId,
            StartDate = inputDataModel.SponsorshipStartDate,
            EndDate = inputDataModel.SponsorshipEndDate,
            Notes = inputDataModel.SponsorshipNotes,
            CreatedById = inputDataModel.CreatedById,
        };
        db.Sponsorships.Add(sponsorship);
        return await db.SaveChangesAsync();
    }

    public async Task<List<AllSponsorshipsDataModel>> GetAllSponsorshipsAsync(int studentId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var query = db.Sponsorships
            .Include(s => s.CreatedBy)
            .Include(s => s.LastUpdatedBy)
            .Include(s => s.DisabledBy)
            .Include(s => s.ReenabledBy)
            .Include(s => s.Sponsor)
            .Include(s => s.Company)
            .Include(s => s.Student)
            .Where(s => s.StudentId == studentId);

        var allSponsorships = await query
            .Select(s => new AllSponsorshipsDataModel(s)
            {
                StudentId = s.StudentId,
                SponsorId = s.SponsorId,
                CompanyId = s.CompanyId,
                SponsorshipStartDate = s.StartDate,
                SponsorshipEndDate = s.EndDate,
                SponsorFullName = s.Sponsor.FullName(),
                StudentFullName = s.Student.FullName(),
                CompanyName = s.Company.Name,
            })
            .ToListAsync();
        return allSponsorships;
    }
}