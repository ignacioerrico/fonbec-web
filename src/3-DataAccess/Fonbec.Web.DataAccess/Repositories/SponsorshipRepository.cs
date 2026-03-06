using Fonbec.Web.DataAccess.DataModels.Sponsorships.Input;
using Fonbec.Web.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Repositories;

public interface ISponsorshipRepository
{
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
}