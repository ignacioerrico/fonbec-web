using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fonbec.Web.DataAccess.DataModels.Sponsors;
using Fonbec.Web.DataAccess.DataModels.Students;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Repositories;

public interface ISponsorRepository
{
    Task<List<AllSponsorsDataModel>> GetAllSponsorsAsync();
}

public class SponsorRepository(IDbContextFactory<FonbecWebDbContext> dbContext) : ISponsorRepository
{
    public async Task<List<AllSponsorsDataModel>> GetAllSponsorsAsync()
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var allSponsors = await db.Sponsors
            .Where(s => s.IsActive)
            .Select(s => new AllSponsorsDataModel(s)
            {
                SponsorId = s.Id,
                SponsorFirstName = s.FirstName,
                SponsorLastName = s.LastName,
                SponsorNickName = s.NickName,
                SponsorGender = s.Gender,
                SponsorEmail = s.Email,
                SponsorPhoneNumber = s.PhoneNumber
            })
            .OrderBy(s => s.SponsorFirstName)
            .ThenBy(s => s.SponsorLastName)
            .ToListAsync();

        return allSponsors;
    }
}

