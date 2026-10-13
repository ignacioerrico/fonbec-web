using Fonbec.Web.DataAccess.DataModels.LetterPlanStatistics;
using Fonbec.Web.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Repositories;

public interface ILetterPlanStatisticsRepository
{
    /// <summary>
    /// Letter timings for a completed plan in the manager's chapter.
    /// Null when the plan is missing, belongs to another chapter, or is still open.
    /// </summary>
    Task<LetterPlanStatisticsQueryResultDataModel?> GetStatisticsAsync(int planId, int chapterId);
}

public class LetterPlanStatisticsRepository(IDbContextFactory<FonbecWebDbContext> dbContext)
    : ILetterPlanStatisticsRepository
{
    public async Task<LetterPlanStatisticsQueryResultDataModel?> GetStatisticsAsync(int planId, int chapterId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var plan = await db.PlannedDeliveries
            .AsNoTracking()
            .Where(p => p.Id == planId && p.IsActive && p.Completed && p.ChapterId == chapterId)
            .Select(p => new { p.StartsOn })
            .FirstOrDefaultAsync();

        if (plan is null)
        {
            return null;
        }

        var letters = await db.Set<Letter>()
            .AsNoTracking()
            .Where(l => l.PlanId == planId && l.ChapterId == chapterId)
            .Select(l => new LetterRow
            {
                SponsorId = l.SponsorId,
                CompanyId = l.CompanyId,
                Letter = new LetterPlanStatisticsLetterDataModel
                {
                    DigitalImprovementStatus = l.DigitalImprovementStatus,
                    Status = l.Status,
                    UploadedOn = l.UploadedOn,
                    ImprovementFirstLockedOn = l.ImprovementFirstLockedOn,
                    DecisionImprovementLockedOn = l.DecisionImprovementLockedOn,
                    ImprovementCompletedOn = l.ImprovementCompletedOn,
                    ReviewFirstLockedOn = l.ReviewFirstLockedOn,
                    DecisionReviewLockedOn = l.DecisionReviewLockedOn,
                    ReviewedOn = l.Review == null ? null : l.Review.ReviewedOn,
                    ReviewerName = l.Review == null
                        ? null
                        : l.Review.ReviewedBy.FirstName + " " + l.Review.ReviewedBy.LastName,
                    RejectedOn = l.RejectedOn,
                    RejectedByName = l.RejectedBy == null
                        ? null
                        : l.RejectedBy.FirstName + " " + l.RejectedBy.LastName,
                    RejectionReason = l.RejectedReason == null ? null : l.RejectedReason.Description,
                    ConfirmedWrittenDate = l.Review == null ? null : l.Review.ConfirmedWrittenDate,
                    SpellingScore = l.Review == null ? null : l.Review.Assessment.SpellingScore,
                    PenmanshipScore = l.Review == null ? null : l.Review.Assessment.PenmanshipScore,
                    ContentScore = l.Review == null ? null : l.Review.Assessment.ContentScore,
                },
                Shares = l.Shares
                    .Select(s => new ShareStamp
                    {
                        SponsorId = s.SponsorId,
                        CompanyId = s.CompanyId,
                        NotificationSentOn = s.NotificationSentOn,
                    })
                    .ToList(),
            })
            .ToListAsync();

        foreach (var row in letters)
        {
            row.Letter.AddresseeNotificationSentOn = row.SponsorId is int sponsorId
                ? row.Shares.FirstOrDefault(s => s.SponsorId == sponsorId)?.NotificationSentOn
                : row.Shares.FirstOrDefault(s => s.CompanyId == row.CompanyId)?.NotificationSentOn;
        }

        var exemptionCount = await db.LetterExemptions
            .AsNoTracking()
            .CountAsync(e => e.PlannedDeliveryId == planId && e.ChapterId == chapterId && !e.IsRevoked);

        return new LetterPlanStatisticsQueryResultDataModel
        {
            PlanStartsOn = plan.StartsOn,
            ExemptionCount = exemptionCount,
            Letters = letters.Select(row => row.Letter).ToList(),
        };
    }

    private sealed class LetterRow
    {
        public int? SponsorId { get; init; }
        public int? CompanyId { get; init; }
        public LetterPlanStatisticsLetterDataModel Letter { get; init; } = null!;
        public List<ShareStamp> Shares { get; init; } = [];
    }

    private sealed class ShareStamp
    {
        public int? SponsorId { get; init; }
        public int? CompanyId { get; init; }
        public DateTime? NotificationSentOn { get; init; }
    }
}