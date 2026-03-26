using Fonbec.Web.DataAccess.DataModels.Sponsors;
using Fonbec.Web.DataAccess.DataModels.Sponsors.Input;
using Fonbec.Web.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Repositories;

public interface ISponsorRepository
{
    /// <summary>
    /// Get all sponsors that have not been (soft) deleted.
    /// </summary>
    /// <param name="chapterId">Use <c>null</c> to get all sponsors for all chapters.</param>
    /// <returns>A list of <see cref="AllSponsorsDataModel"/></returns>
    Task<List<AllSponsorsDataModel>> GetAllSponsorsAsync(int? chapterId);

    Task<int> CreateSponsorAsync(CreateSponsorInputDataModel dataModel);

    Task<int> UpdateSponsorAsync(UpdateSponsorInputDataModel dataModel);

    /// <summary>
    /// Links a group of sponsors to a company.
    /// </summary>
    /// <param name="sponsorsIds">IDs of the sponsors to link</param>
    /// <param name="companyId">ID of the company to link to</param>
    /// <returns></returns>
    Task<int> LinkSponsorsToCompanyAsync(List<int> sponsorsIds, int companyId);
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
            .Include(s => s.Company)
            .Where(s => !s.IsDeleted
                        && (!chapterId.HasValue || s.ChapterId == chapterId))
            .Select(s => new AllSponsorsDataModel(s)
            {
                SponsorId = s.Id,
                SponsorFirstName = s.FirstName,
                SponsorLastName = s.LastName,
                SponsorNickName = s.NickName,
                SponsorGender = s.Gender,
                SponsorPhoneNumber = s.PhoneNumber,
                SponsorEmail = s.Email,
                IsSponsorActive = s.IsActive,
                SponsorCompany = s.Company,
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
            Email = dataModel.SponsorEmail,
            PhoneNumber = dataModel.SponsorPhoneNumber,
            CompanyId = dataModel.SponsorCompanyId,
            Notes = dataModel.SponsorNotes,
            CreatedById = dataModel.CreatedById,
        };

        db.Sponsors.Add(sponsor);
        return await db.SaveChangesAsync();
    }

    public async Task<int> UpdateSponsorAsync(UpdateSponsorInputDataModel dataModel)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var sponsorDb = await db.Sponsors.FindAsync(dataModel.SponsorId);

        if (sponsorDb is not { IsActive: true })
        {
            return 0;
        }

        sponsorDb.FirstName = dataModel.SponsorFirstName;
        sponsorDb.LastName = dataModel.SponsorLastName;
        sponsorDb.NickName = dataModel.SponsorNickName;
        sponsorDb.Gender = dataModel.SponsorGender;
        sponsorDb.PhoneNumber = dataModel.SponsorPhoneNumber;
        sponsorDb.Email = dataModel.SponsorEmail;
        sponsorDb.CompanyId = dataModel.SponsorCompanyId;
        sponsorDb.LastUpdatedById = dataModel.UpdatedById;

        db.Sponsors.Update(sponsorDb);
        return await db.SaveChangesAsync();
    }

    public async Task<int> LinkSponsorsToCompanyAsync(List<int> sponsorIds, int companyId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var sponsorsDb = await db.Sponsors
            .Where(s => sponsorIds.Contains(s.Id))
            .ToListAsync();

        sponsorsDb.ForEach(s => s.CompanyId = companyId);

        db.Sponsors.UpdateRange(sponsorsDb);
        return await db.SaveChangesAsync();
    }
}