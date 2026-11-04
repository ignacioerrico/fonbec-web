using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.DataModels.Chapters;
using Fonbec.Web.DataAccess.DataModels.Chapters.Input;
using Fonbec.Web.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Repositories;

public interface IChapterRepository
{
    Task<string?> GetChapterNameAsync(int chapterId);
    Task<List<AllChaptersDataModel>> GetAllChaptersAsync();
    Task<ChapterDirectoryDataModel> GetChapterDirectoryAsync();
    Task<int> CreateChapterAsync(CreateChapterInputDataModel dataModel);
    Task<int> UpdateChapterAsync(UpdateChapterInputDataModel dataModel);
}

public class ChapterRepository(IDbContextFactory<FonbecWebDbContext> dbContext) : IChapterRepository
{
    public async Task<string?> GetChapterNameAsync(int chapterId)
    {
        await using var db = await dbContext.CreateDbContextAsync();
        
        return await db.Chapters
            .AsNoTracking()
            .Where(ch => ch.Id == chapterId && ch.IsActive)
            .Select(ch => ch.Name)
            .FirstOrDefaultAsync();
    }

    public async Task<List<AllChaptersDataModel>> GetAllChaptersAsync()
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var allChapters = await db.Chapters
            .AsNoTracking()
            .Include(ch => ch.CreatedBy)
            .Include(ch => ch.LastUpdatedBy)
            .Include(ch => ch.DisabledBy)
            .Include(ch => ch.ReenabledBy)
            .Where(ch => ch.IsActive)
            .Select(ch => new AllChaptersDataModel(ch)
            {
                ChapterId = ch.Id,
                ChapterName = ch.Name,
                Notes = ch.Notes,
                IsChapterActive = ch.IsActive
            })
            .OrderBy(ch => ch.ChapterName)
            .ToListAsync();

        return allChapters;
    }

    public async Task<ChapterDirectoryDataModel> GetChapterDirectoryAsync()
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var now = DateTimeOffset.Now;

        var staff = await (
            from user in db.Users.AsNoTracking()
            where user.ChapterId != null
            join chapter in db.Chapters.AsNoTracking() on user.ChapterId equals (int?)chapter.Id
            where chapter.IsActive
            join userRole in db.UserRoles on user.Id equals userRole.UserId
            join role in db.Roles on userRole.RoleId equals role.Id
            where role.Name == FonbecRole.Manager
                  || role.Name == FonbecRole.Uploader
                  || role.Name == FonbecRole.Reviewer
            select new ChapterStaffMemberDataModel
            {
                ChapterId = user.ChapterId!.Value,
                UserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                NickName = user.NickName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                Role = role.Name!,
                CanBeLockedOut = user.LockoutEnabled,
                IsActive = !user.LockoutEnabled
                           || user.LockoutEnd == null
                           || user.LockoutEnd < now,
            }).ToListAsync();

        var uploaderIds = staff
            .Where(member => member.Role == FonbecRole.Uploader)
            .Select(member => member.UserId)
            .ToList();

        if (uploaderIds.Count > 0)
        {
            var linkedFacilitatorIds = await db.Students
                .AsNoTracking()
                .Where(student => uploaderIds.Contains(student.FacilitatorId))
                .Select(student => student.FacilitatorId)
                .Distinct()
                .ToListAsync();

            var linked = linkedFacilitatorIds.ToHashSet();
            foreach (var member in staff.Where(member => member.Role == FonbecRole.Uploader))
            {
                member.HasLinkedStudents = linked.Contains(member.UserId);
            }
        }

        var studentCounts = await db.Students
            .AsNoTracking()
            .Where(student => student.IsActive)
            .GroupBy(student => student.ChapterId)
            .Select(group => new { ChapterId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.ChapterId, group => group.Count);

        var sponsorCounts = await db.Sponsors
            .AsNoTracking()
            .Where(sponsor => !sponsor.IsDeleted)
            .GroupBy(sponsor => sponsor.ChapterId)
            .Select(group => new { ChapterId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(group => group.ChapterId, group => group.Count);

        return new ChapterDirectoryDataModel
        {
            Staff = staff,
            StudentCounts = studentCounts,
            SponsorCounts = sponsorCounts,
        };
    }

    public async Task<int> CreateChapterAsync(CreateChapterInputDataModel dataModel)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var chapter = new Chapter
        {
            Name = dataModel.ChapterName,
            Notes = dataModel.ChapterNotes,
            CreatedById = dataModel.ChapterCreatedById,
        };
        
        db.Chapters.Add(chapter);
        return await db.SaveChangesAsync();
    }

    public async Task<int> UpdateChapterAsync(UpdateChapterInputDataModel dataModel)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var chapterDb = await db.Chapters.FindAsync(dataModel.ChapterId);

        if (chapterDb is not { IsActive: true })
        {
            return 0;
        }

        chapterDb.Name = dataModel.ChapterUpdatedName;
        chapterDb.Notes = dataModel.ChapterUpdatedNotes;
        chapterDb.LastUpdatedById = dataModel.UpdatedById;

        db.Chapters.Update(chapterDb);
        return await db.SaveChangesAsync();
    }
}
