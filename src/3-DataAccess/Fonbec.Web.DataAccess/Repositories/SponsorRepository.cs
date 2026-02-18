using Fonbec.Web.DataAccess.DataModels.Sponsors;
using Fonbec.Web.DataAccess.DataModels.Sponsors.Input;
using Fonbec.Web.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

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

        var allSponsors = await db.Sponsors
            .Include(s => s.CreatedBy)
            .Include(s => s.LastUpdatedBy)
            .Include(s => s.DisabledBy)
            .Include(s => s.ReenabledBy)
            .Where(s => !s.IsDeleted
                        && (!chapterId.HasValue || s.ChapterId == chapterId))
            .Select(s => new AllSponsorsDataModel(s)
            {
                SponsorId = s.Id,
                SponsorFirstName = s.FirstName,
                SponsorLastName = s.LastName,
                SponsorNickName = s.NickName,
                SponsorPhoneNumber = s.PhoneNumber,
                SponsorEmail = s.Email,
                IsSponsorActive = s.IsActive,
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