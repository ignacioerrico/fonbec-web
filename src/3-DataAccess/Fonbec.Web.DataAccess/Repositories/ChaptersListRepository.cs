using Fonbec.Web.DataAccess.DataModels.Chapters;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Repositories;

public interface IChaptersListRepository
{
    Task<List<ChaptersListDataModel>> GetAllChaptersAsync();
}

public class ChaptersListRepository(IDbContextFactory<FonbecWebDbContext> dbContext) : IChaptersListRepository
{
    public async Task<List<ChaptersListDataModel>> GetAllChaptersAsync()
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var allChapters = await db.Chapters
            .OrderBy(ch => ch.Name)
            .Select(ch => new ChaptersListDataModel
            {
                ChapterName = ch.Name
            })
            .ToListAsync();
        
        return allChapters;
    }
}