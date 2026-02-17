using Fonbec.Web.DataAccess.DataModels.Sponsors.Input;
using Microsoft.EntityFrameworkCore;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.DataModels.Sponsors;

namespace Fonbec.Web.DataAccess.Repositories;

public interface ISponsorRepository
{
    // falta pasarle filtro chapterId
    Task<List<AllSponsorsDataModel>> GetAllSponsorsAsync(int? chapterId);
    Task<int> CreateSponsorAsync(CreateSponsorInputDataModel dataModel);
}

public class SponsorRepository(IDbContextFactory<FonbecWebDbContext> dbContext) : ISponsorRepository
{
    public async Task<List<AllSponsorsDataModel>> GetAllSponsorsAsync(int? chapterId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var query = db.Sponsors
            .Where(s => s.IsDeleted == false);
        if (chapterId.HasValue)
        {
            query = query.Where(s => s.ChapterId == chapterId);
        }
        var allSponsors = await query
            .Include(s => s.CreatedBy)
            .Include(s => s.LastUpdatedBy)
            .Include(s => s.DisabledBy)
            .Include(s => s.ReenabledBy)
            .Select(s => new AllSponsorsDataModel(s)
            {
                SponsorId = s.Id,
                SponsorFirstName = s.FirstName,
                SponsorLastName = s.LastName,
                SponsorNickName = s.NickName,
                SponsorPhoneNumber = s.PhoneNumber,
                SponsorEmail = s.Email
            })
            .OrderBy(sdm => sdm.SponsorFirstName)
            .ThenBy(sdm => sdm.SponsorLastName)
            .ToListAsync();

        return allSponsors;
    }

    public async Task<int> CreateSponsorAsync(CreateSponsorInputDataModel dataModel)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var sponsor = new Sponsor
        {
            ChapterId = dataModel.ChapterId,
            FirstName = dataModel.SponsorFirstName,
            LastName = dataModel.SponsorLastName,
            NickName = dataModel.SponsorNickName,
            Gender = dataModel.SponsorGender,
            PhoneNumber = dataModel.SponsorPhoneNumber,
            Notes = dataModel.SponsorNotes,
            Email = dataModel.SponsorEmail,
            CreatedById = dataModel.CreatedById
        };

        db.Sponsors.Add(sponsor);
        return await db.SaveChangesAsync();
    }
}
