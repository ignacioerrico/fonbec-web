using Fonbec.Web.DataAccess.DataModels.Sponsors.Input;
using Microsoft.EntityFrameworkCore;
using Fonbec.Web.DataAccess.Entities;

namespace Fonbec.Web.DataAccess.Repositories;

public interface ISponsorRepository
{
    Task<int> CreateSponsorAsync(CreateSponsorInputDataModel dataModel);
}
public class SponsorRepository(IDbContextFactory<FonbecWebDbContext> dbContext) : ISponsorRepository
{
    public async Task<int> CreateSponsorAsync(CreateSponsorInputDataModel dataModel)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var Sponsor = new Sponsor
        {
            ChapterId = dataModel.ChapterId,
            FirstName = dataModel.SponsorFirstName,
            LastName = dataModel.SponsorLastName,
            NickName = dataModel.SponsorNickName,
            Gender = dataModel.SponsorGender,
            PhoneNumber = dataModel.SponsorPhoneNumber,
            Notes = dataModel.SponsorNotes,
            Email = dataModel.SponsorEmail,
            SendAlsoTo = dataModel.SponsorSendAlsoTo,
            BranchOffice = dataModel.SponsorBranchOffice,
            CreatedById = dataModel.CreatedById
        };

        db.Sponsors.Add(Sponsor);
        return await db.SaveChangesAsync();
    }
}
