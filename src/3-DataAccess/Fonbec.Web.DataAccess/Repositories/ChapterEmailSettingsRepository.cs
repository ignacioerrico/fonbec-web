using Fonbec.Web.DataAccess.DataModels.Chapters;
using Fonbec.Web.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Repositories;

public interface IChapterEmailSettingsRepository
{
    Task<ChapterEmailSettingsDataModel?> GetAsync(int chapterId, CancellationToken cancellationToken = default);

    Task<bool> SaveAsync(
        int chapterId,
        int updatedById,
        string subjectPrefix,
        IReadOnlyList<ChapterEmailTemplateDataModel> templates,
        CancellationToken cancellationToken = default);
}

public class ChapterEmailSettingsRepository(IDbContextFactory<FonbecWebDbContext> dbContext)
    : IChapterEmailSettingsRepository
{
    public async Task<ChapterEmailSettingsDataModel?> GetAsync(
        int chapterId, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContext.CreateDbContextAsync(cancellationToken);

        return await db.Chapters
            .AsNoTracking()
            .Where(chapter => chapter.Id == chapterId)
            .Select(chapter => new ChapterEmailSettingsDataModel
            {
                ChapterName = chapter.Name,
                SubjectPrefix = chapter.EmailSubjectPrefix,
                Templates = chapter.EmailTemplates
                    .Select(template => new ChapterEmailTemplateDataModel
                    {
                        Kind = template.Kind,
                        Subject = template.Subject,
                        Body = template.Body,
                    })
                    .ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> SaveAsync(
        int chapterId,
        int updatedById,
        string subjectPrefix,
        IReadOnlyList<ChapterEmailTemplateDataModel> templates,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContext.CreateDbContextAsync(cancellationToken);
        var chapter = await db.Chapters
            .Include(item => item.EmailTemplates)
            .FirstOrDefaultAsync(item => item.Id == chapterId && item.IsActive, cancellationToken);
        if (chapter is null)
        {
            return false;
        }

        chapter.EmailSubjectPrefix = subjectPrefix.Trim();
        chapter.LastUpdatedById = updatedById;

        foreach (var template in templates)
        {
            var existing = chapter.EmailTemplates.FirstOrDefault(item => item.Kind == template.Kind);
            if (existing is null)
            {
                chapter.EmailTemplates.Add(new ChapterEmailTemplate
                {
                    Kind = template.Kind,
                    Subject = template.Subject.Trim(),
                    Body = template.Body.Trim(),
                });
                continue;
            }

            existing.Subject = template.Subject.Trim();
            existing.Body = template.Body.Trim();
        }

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}