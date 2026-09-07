using Fonbec.Web.DataAccess.DataModels.LetterFollowUp;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Repositories;

public interface ILetterFollowUpRepository
{
    Task<LetterFollowUpQueryResultDataModel> GetOpenTasksAsync(int chapterId);

    Task<bool> ResolveRedFlagAsync(
        long assessmentId, int chapterId, int resolvedById, DateTime resolvedOn);

    Task<bool> ResolveGreenFlagAsync(
        long assessmentId, int chapterId, int resolvedById, DateTime resolvedOn);

    Task<bool> SetRedFlagPriorityAsync(
        long assessmentId, int chapterId, RedFlagPriority priority);
}

public sealed class LetterFollowUpRepository(
    IDbContextFactory<FonbecWebDbContext> dbContext) : ILetterFollowUpRepository
{
    public async Task<LetterFollowUpQueryResultDataModel> GetOpenTasksAsync(int chapterId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var reviews = db.LetterReviews
            .AsNoTracking()
            .Where(review =>
                review.Document.ChapterId == chapterId
                && review.Document.Status == DocumentStatus.Approved);

        var redFlags = await reviews
            .Where(review =>
                review.Assessment.HasRedFlags
                && !review.Assessment.IsRedFlagResolved)
            .Select(review => new LetterFollowUpTaskDataModel
            {
                AssessmentId = review.AssessmentId,
                StudentFirstName = review.Document.Student.FirstName,
                StudentLastName = review.Document.Student.LastName,
                FacilitatorFirstName = review.Document.Student.Facilitator.FirstName,
                FacilitatorLastName = review.Document.Student.Facilitator.LastName,
                FacilitatorEmail = review.Document.Student.Facilitator.Email ?? string.Empty,
                ReviewerFirstName = review.ReviewedBy.FirstName,
                ReviewerLastName = review.ReviewedBy.LastName,
                ReviewerEmail = review.ReviewedBy.Email ?? string.Empty,
                ReportedOn = review.ReviewedOn,
                Comment = review.Assessment.IssuesNotes ?? string.Empty,
                Priority = review.Assessment.RedFlagPriority,
            })
            .OrderByDescending(task => task.Priority)
            .ThenByDescending(task => task.ReportedOn)
            .ThenBy(task => task.AssessmentId)
            .ToListAsync();

        var greenFlags = await reviews
            .Where(review =>
                review.Assessment.HasGreenFlags
                && !review.Assessment.IsGreenFlagResolved)
            .Select(review => new LetterFollowUpTaskDataModel
            {
                AssessmentId = review.AssessmentId,
                StudentFirstName = review.Document.Student.FirstName,
                StudentLastName = review.Document.Student.LastName,
                FacilitatorFirstName = review.Document.Student.Facilitator.FirstName,
                FacilitatorLastName = review.Document.Student.Facilitator.LastName,
                FacilitatorEmail = review.Document.Student.Facilitator.Email ?? string.Empty,
                ReviewerFirstName = review.ReviewedBy.FirstName,
                ReviewerLastName = review.ReviewedBy.LastName,
                ReviewerEmail = review.ReviewedBy.Email ?? string.Empty,
                ReportedOn = review.ReviewedOn,
                Comment = review.Assessment.Appraisal ?? string.Empty,
            })
            .OrderByDescending(task => task.ReportedOn)
            .ThenBy(task => task.AssessmentId)
            .ToListAsync();

        return new LetterFollowUpQueryResultDataModel
        {
            RedFlags = redFlags,
            GreenFlags = greenFlags,
        };
    }

    public async Task<bool> ResolveRedFlagAsync(
        long assessmentId, int chapterId, int resolvedById, DateTime resolvedOn)
    {
        await using var db = await dbContext.CreateDbContextAsync();
        var assessment = await GetOpenAssessmentAsync(
            db, assessmentId, chapterId, redFlag: true);

        if (assessment is null)
        {
            return false;
        }

        assessment.IsRedFlagResolved = true;
        assessment.RedFlagResolvedById = resolvedById;
        assessment.RedFlagResolvedOn = resolvedOn;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ResolveGreenFlagAsync(
        long assessmentId, int chapterId, int resolvedById, DateTime resolvedOn)
    {
        await using var db = await dbContext.CreateDbContextAsync();
        var assessment = await GetOpenAssessmentAsync(
            db, assessmentId, chapterId, redFlag: false);

        if (assessment is null)
        {
            return false;
        }

        assessment.IsGreenFlagResolved = true;
        assessment.GreenFlagResolvedById = resolvedById;
        assessment.GreenFlagResolvedOn = resolvedOn;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetRedFlagPriorityAsync(
        long assessmentId, int chapterId, RedFlagPriority priority)
    {
        await using var db = await dbContext.CreateDbContextAsync();
        var assessment = await GetOpenAssessmentAsync(
            db, assessmentId, chapterId, redFlag: true);

        if (assessment is null)
        {
            return false;
        }

        assessment.RedFlagPriority = priority;
        await db.SaveChangesAsync();
        return true;
    }

    private static Task<Assessment?> GetOpenAssessmentAsync(
        FonbecWebDbContext db, long assessmentId, int chapterId, bool redFlag)
    {
        return db.Assessments.FirstOrDefaultAsync(assessment =>
            assessment.AssessmentId == assessmentId
            && assessment.LetterReview != null
            && assessment.LetterReview.Document.ChapterId == chapterId
            && assessment.LetterReview.Document.Status == DocumentStatus.Approved
            && (redFlag
                ? assessment.HasRedFlags && !assessment.IsRedFlagResolved
                : assessment.HasGreenFlags && !assessment.IsGreenFlagResolved));
    }
}