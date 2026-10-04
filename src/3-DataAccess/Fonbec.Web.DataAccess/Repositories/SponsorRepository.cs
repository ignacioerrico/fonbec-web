using Fonbec.Web.DataAccess.DataModels.Review;
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
    /// Load a sponsor and its additional recipients when the sponsor is in <paramref name="chapterId"/>.
    /// Pass <c>null</c> to allow any chapter. Soft-deleted sponsors are omitted.
    /// </summary>
    Task<SponsorSendAlsoTosDataModel?> GetSendAlsoTosBySponsorIdAsync(int sponsorId, int? chapterId);

    /// <summary>
    /// Replace the sponsor's additional recipients: update matching rows, insert new ones, and delete rows that are no longer present.
    /// </summary>
    Task<UpdateSendAlsoTosRepositoryResult> UpdateSendAlsoTosAsync(UpdateSponsorSendAlsoTosInputDataModel dataModel);

    /// <summary>Get a single sponsor's name, or <c>null</c> when the sponsor does not exist.</summary>
    Task<CandidateNameDataModel?> GetSponsorNameAsync(int sponsorId);

    /// <summary>
    /// Get every non-deleted sponsor name except <paramref name="excludeSponsorId"/>, ordered by id.
    /// Not chapter-restricted. The review picker samples this pool deterministically.
    /// </summary>
    Task<List<CandidateNameDataModel>> GetSponsorCandidateNamesAsync(int? excludeSponsorId);
}

public class SponsorRepository(IDbContextFactory<FonbecWebDbContext> dbContext) : ISponsorRepository
{
    public async Task<List<AllSponsorsDataModel>> GetAllSponsorsAsync(int? chapterId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var allSponsors = await db.Sponsors
            .AsNoTracking()
            .Include(s => s.CreatedBy)
            .Include(s => s.LastUpdatedBy)
            .Include(s => s.DisabledBy)
            .Include(s => s.ReenabledBy)
            .Include(s => s.Company)
            .Include(s => s.Chapter)
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
                SponsorChapterName = s.Chapter.Name,
                SponsoredStudents = s.Sponsorships
                    .Where(sp => sp.IsActive
                                 && sp.Student.IsActive
                                 && !sp.Student.IsDeleted)
                    .OrderBy(sp => sp.Student.LastName)
                    .ThenBy(sp => sp.Student.FirstName)
                    .ThenBy(sp => sp.StartDate)
                    .Select(sp => new SponsoredStudentDataModel
                    {
                        Name = sp.Student.FirstName + " " + sp.Student.LastName,
                        StartDate = sp.StartDate,
                        EndDate = sp.EndDate,
                    })
                    .ToList(),
                SendAlsoTos = s.SendAlsoTos
                    .OrderBy(r => r.SendAsBcc)
                    .ThenBy(r => r.RecipientName)
                    .Select(r => new SponsorListRecipientDataModel
                    {
                        Name = r.RecipientName,
                        Email = r.RecipientEmail,
                        SendAsBcc = r.SendAsBcc,
                    })
                    .ToList(),
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
            PublicAccessToken = Guid.NewGuid(),
            SendAlsoTos = (dataModel.SendAlsoTos ?? []).Select(recipient => new SendAlsoTo
            {
                RecipientName = recipient.RecipientName,
                RecipientEmail = recipient.RecipientEmail,
                SendAsBcc = recipient.SendAsBcc,
                CreatedById = dataModel.CreatedById,
            }).ToList(),
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

    public async Task<SponsorSendAlsoTosDataModel?> GetSendAlsoTosBySponsorIdAsync(int sponsorId, int? chapterId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        return await db.Sponsors
            .AsNoTracking()
            .Where(s => s.Id == sponsorId
                        && !s.IsDeleted
                        && (!chapterId.HasValue || s.ChapterId == chapterId))
            .Select(s => new SponsorSendAlsoTosDataModel
            {
                SponsorId = s.Id,
                SponsorFirstName = s.FirstName,
                SponsorLastName = s.LastName,
                SponsorEmail = s.Email,
                IsSponsorActive = s.IsActive,
                Recipients = s.SendAlsoTos
                    .OrderBy(r => r.RecipientName)
                    .ThenBy(r => r.Id)
                    .Select(r => new SendAlsoToDataModel
                    {
                        Id = r.Id,
                        RecipientName = r.RecipientName,
                        RecipientEmail = r.RecipientEmail,
                        SendAsBcc = r.SendAsBcc,
                    })
                    .ToList(),
            })
            .SingleOrDefaultAsync();
    }

    public async Task<UpdateSendAlsoTosRepositoryResult> UpdateSendAlsoTosAsync(UpdateSponsorSendAlsoTosInputDataModel dataModel)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var sponsor = await db.Sponsors
            .Include(s => s.SendAlsoTos)
            .SingleOrDefaultAsync(s =>
                s.Id == dataModel.SponsorId
                && !s.IsDeleted
                && s.IsActive
                && (!dataModel.ChapterId.HasValue || s.ChapterId == dataModel.ChapterId));

        if (sponsor is null)
        {
            return new UpdateSendAlsoTosRepositoryResult(SponsorFound: false, AffectedRows: 0);
        }

        var existingById = sponsor.SendAlsoTos.ToDictionary(r => r.Id);
        if (dataModel.Recipients.Any(r => r.Id > 0 && !existingById.ContainsKey(r.Id)))
        {
            return new UpdateSendAlsoTosRepositoryResult(SponsorFound: true, AffectedRows: 0, Rejected: true);
        }

        var incomingIds = dataModel.Recipients
            .Where(r => r.Id > 0)
            .Select(r => r.Id)
            .ToHashSet();

        foreach (var existing in sponsor.SendAlsoTos.Where(r => !incomingIds.Contains(r.Id)).ToList())
        {
            db.SendAlsoTos.Remove(existing);
        }

        foreach (var recipient in dataModel.Recipients)
        {
            if (recipient.Id > 0)
            {
                var existing = existingById[recipient.Id];
                if (existing.RecipientName == recipient.RecipientName
                    && existing.RecipientEmail == recipient.RecipientEmail
                    && existing.SendAsBcc == recipient.SendAsBcc)
                {
                    continue;
                }

                existing.RecipientName = recipient.RecipientName;
                existing.RecipientEmail = recipient.RecipientEmail;
                existing.SendAsBcc = recipient.SendAsBcc;
                existing.LastUpdatedById = dataModel.UpdatedById;
                continue;
            }

            sponsor.SendAlsoTos.Add(new SendAlsoTo
            {
                RecipientName = recipient.RecipientName,
                RecipientEmail = recipient.RecipientEmail,
                SendAsBcc = recipient.SendAsBcc,
                CreatedById = dataModel.UpdatedById,
            });
        }

        var affectedRows = await db.SaveChangesAsync();
        return new UpdateSendAlsoTosRepositoryResult(SponsorFound: true, AffectedRows: affectedRows);
    }

    public async Task<CandidateNameDataModel?> GetSponsorNameAsync(int sponsorId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        return await db.Sponsors
            .AsNoTracking()
            .Where(s => s.Id == sponsorId)
            .Select(s => new CandidateNameDataModel
            {
                Id = s.Id,
                FirstName = s.FirstName,
                LastName = s.LastName,
                IsCompany = false,
            })
            .FirstOrDefaultAsync();
    }

    public async Task<List<CandidateNameDataModel>> GetSponsorCandidateNamesAsync(int? excludeSponsorId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var query = db.Sponsors
            .AsNoTracking()
            .Where(s => !s.IsDeleted);

        if (excludeSponsorId.HasValue)
        {
            query = query.Where(s => s.Id != excludeSponsorId.Value);
        }

        return await query
            .OrderBy(s => s.Id)
            .Select(s => new CandidateNameDataModel
            {
                Id = s.Id,
                FirstName = s.FirstName,
                LastName = s.LastName,
                IsCompany = false,
            })
            .ToListAsync();
    }
}