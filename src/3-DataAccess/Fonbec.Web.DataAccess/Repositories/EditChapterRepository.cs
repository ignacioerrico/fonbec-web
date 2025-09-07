using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fonbec.Web.DataAccess.Entities;

namespace Fonbec.Web.DataAccess.Repositories
{
    public interface IEditChapterRepository
    {
        Task<Chapter> EditChapterAsync(Chapter chapter);
    }

    public class EditChapterRepository(IDbContextFactory<FonbecWebDbContext> dbContext) : IEditChapterRepository
    {
        public async Task<Chapter> EditChapterAsync(Chapter chapterToEdit)
        {
            await using var db = await dbContext.CreateDbContextAsync();
            var chapter = await db.Chapters.FindAsync(chapterToEdit.Id);
            if (chapter == null)
            {
                throw new Exception("Chapter not found");
            }
            chapter.Name = chapterToEdit.Name;
            db.Chapters.Update(chapter);
            await db.SaveChangesAsync();
            return chapter;
        }
    }
}
