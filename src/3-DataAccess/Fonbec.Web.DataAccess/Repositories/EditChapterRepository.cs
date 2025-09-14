using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.DataModels.Chapters;

namespace Fonbec.Web.DataAccess.Repositories
{
    public interface IEditChapterRepository
    {
        Task<Chapter> EditChapterAsync(EditChapterInputDataModel chapter);
    }

    public class EditChapterRepository(IDbContextFactory<FonbecWebDbContext> dbContext) : IEditChapterRepository
    {
        public async Task<Chapter> EditChapterAsync(EditChapterInputDataModel chapterToEdit)
        {
            await using var db = await dbContext.CreateDbContextAsync();
            var chapter = await db.Chapters.FindAsync(chapterToEdit.ChapterID);
            if (chapter == null)
            {
                throw new Exception("Chapter not found");
            }
            chapter.Name = chapterToEdit.ChapterName;
            db.Chapters.Update(chapter);
            await db.SaveChangesAsync();
            return chapter;
        }
    }
}
