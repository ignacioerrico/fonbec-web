using Fonbec.Web.DataAccess.DataModels.Sponsorships;
using Fonbec.Web.DataAccess.DataModels.Sponsorships.Input;
using Fonbec.Web.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Repositories;

public interface ISponsorshipRepository
{
    Task<AllSponsorshipsDataModel> GetAllSponsorshipsAsync(int studentId);
    Task<int> CreateSponsorshipAsync(CreateSponsorshipInputDataModel inputDataModel);
}

public class SponsorshipRepository(IDbContextFactory<FonbecWebDbContext> dbContext) : ISponsorshipRepository
{
    public async Task<AllSponsorshipsDataModel> GetAllSponsorshipsAsync(int studentId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var allSponsorshipsForStudent = await db.Sponsorships
            .Include(s => s.Student)
            .Include(s => s.Sponsor)
            .ThenInclude(s => s.Company)
            .Include(s => s.Company)
            .Include(s => s.CreatedBy)
            .Include(s => s.LastUpdatedBy)
            .Include(s => s.DisabledBy)
            .Include(s => s.ReenabledBy)
            .Where(s => s.StudentId == studentId)
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

    public async Task<int> CreateSponsorshipAsync(CreateSponsorshipInputDataModel inputDataModel)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var sponsorship = new Sponsorship
        {
            StudentId = inputDataModel.StudentId,
            SponsorId = inputDataModel.SponsorId,
            CompanyId = inputDataModel.CompanyId,
            StartDate = inputDataModel.SponsorshipStartDate,
            EndDate = inputDataModel.SponsorshipEndDate,
            Notes = inputDataModel.SponsorshipNotes,
            CreatedById = inputDataModel.CreatedById,
        };
        db.Sponsorships.Add(sponsorship);
        return await db.SaveChangesAsync();
    }
}