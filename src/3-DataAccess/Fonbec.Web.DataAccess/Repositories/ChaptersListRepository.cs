using Fonbec.Web.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Repositories;

public interface IChaptersListRepository
{
    Task<List<Chapter>> GetAllChaptersAsync();
}

public class ChaptersListRepository : IChaptersListRepository
{
    private readonly FonbecWebDbContext _dbContext;

    public ChaptersListRepository(FonbecWebDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Chapter>> GetAllChaptersAsync()
    {
        return await _dbContext.Chapters
            .OrderBy(ch => ch.Name)
            .ToListAsync();
    }
}