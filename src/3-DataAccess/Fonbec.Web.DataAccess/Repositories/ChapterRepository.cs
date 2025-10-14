using Fonbec.Web.DataAccess.DataModels.Chapters;
using Fonbec.Web.DataAccess.DataModels.Chapters.Input;
using Fonbec.Web.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Repositories;

public interface IChapterRepository
{
    Task<string?> GetChapterNameAsync(int chapterId);
    Task<List<AllChaptersDataModel>> GetAllChaptersAsync();
    Task<int> CreateChapterAsync(CreateChapterInputDataModel dataModel);
    Task<int> UpdateChapterAsync(UpdateChapterInputDataModel dataModel);
}

public class ChapterRepository(IDbContextFactory<FonbecWebDbContext> dbContext) : IChapterRepository
{
    public async Task<string?> GetChapterNameAsync(int chapterId)
    {
        await using var db = await dbContext.CreateDbContextAsync();
        
        var chapter = await db.Chapters.FindAsync(chapterId);
            
        return chapter is { IsActive: true }
            ? chapter.Name
            : null;
    }

    public async Task<List<AllChaptersDataModel>> GetAllChaptersAsync()
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var allChapters = await db.Chapters
            .Include(ch => ch.CreatedBy)
            .Include(ch => ch.LastUpdatedBy)
            .Include(ch => ch.DisabledBy)
            .Include(ch => ch.ReenabledBy)
            .Where(ch => ch.IsActive)
            .Select(ch => new AllChaptersDataModel(ch)
            {
                ChapterId = ch.Id,
                ChapterName = ch.Name,
                IsChapterActive = ch.IsActive
            })
            .OrderBy(ch => ch.ChapterName)
            .ToListAsync();

        return allChapters;
    }

    public async Task<int> CreateChapterAsync(CreateChapterInputDataModel dataModel)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var chapter = new Chapter
        {
            Name = dataModel.ChapterName,
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

        db.Chapters.Update(chapterDb);
        return await db.SaveChangesAsync();
    }
}