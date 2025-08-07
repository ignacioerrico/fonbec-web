using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fonbec.Web.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Repositories
{
    public interface ICreateChapterRepository
    {
        Task<Chapter> CreateChapterAsync(Chapter chapterData);
    }
    public class CreateChapterRepository(IDbContextFactory<FonbecWebDbContext> dbContext) : ICreateChapterRepository
    {
        public async Task<Chapter> CreateChapterAsync(Chapter chapterData)
        {
            await using var db = await dbContext.CreateDbContextAsync();

            var chapter = new Chapter(chapterData.Name);
            db.Chapters.Add(chapter);
            await db.SaveChangesAsync();
            return chapter;
        }
    }
}
