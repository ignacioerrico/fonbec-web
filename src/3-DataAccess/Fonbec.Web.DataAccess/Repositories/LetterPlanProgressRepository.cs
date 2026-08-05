using Fonbec.Web.DataAccess.DataModels.LetterPlanProgress;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Repositories;

public interface ILetterPlanProgressRepository
{
    Task<LetterPlanProgressQueryResultDataModel?> GetProgressAsync(int planId, int chapterId);
}

public class LetterPlanProgressRepository(
    IDbContextFactory<FonbecWebDbContext> dbContext,
    TimeProvider timeProvider) : ILetterPlanProgressRepository
{
    public async Task<LetterPlanProgressQueryResultDataModel?> GetProgressAsync(int planId, int chapterId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var plan = await db.PlannedDeliveries
            .AsNoTracking()
            .Where(p => p.Id == planId && p.IsActive)
            .Select(p => new
            {
                p.ChapterId,
                p.StartsOn,
                p.Completed,
            })
            .FirstOrDefaultAsync();

        if (plan is null || plan.ChapterId != chapterId)
        {
            return null;
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var exemptions = await db.LetterExemptions
            .AsNoTracking()
            .Where(e => e.PlannedDeliveryId == planId && !e.IsRevoked)
            .ToDictionaryAsync(e => e.StudentId, e => e.Reason);

        // NOTE: the sponsorship predicate below mirrors FacilitatorRepository; keep both in sync.
        var slots = await db.Students
            .AsNoTracking()
            .Where(s => s.ChapterId == chapterId
                        && s.IsActive
                        && !s.IsDeleted
                        && s.Sponsorships.Any(sp =>
                            sp.IsActive
                            && sp.StartDate <= utcNow
                            && (sp.EndDate == null || sp.EndDate >= utcNow)
                            && (
                                (sp.SponsorId != null
                                 && sp.Sponsor != null
                                 && sp.Sponsor.IsActive
                                 && !sp.Sponsor.IsDeleted)
                                || (sp.CompanyId != null
                                    && sp.Company != null
                                    && sp.Company.IsActive))))
            .SelectMany(s => s.Sponsorships
                .Where(sp =>
                    sp.IsActive
                    && sp.StartDate <= utcNow
                    && (sp.EndDate == null || sp.EndDate >= utcNow)
                    && (
                        (sp.SponsorId != null
                         && sp.Sponsor != null
                         && sp.Sponsor.IsActive
                         && !sp.Sponsor.IsDeleted)
                        || (sp.CompanyId != null
                            && sp.Company != null
                            && sp.Company.IsActive)))
                .Select(sp => new
                {
                    s.Id,
                    s.FirstName,
                    s.LastName,
                    s.NickName,
                    FacilitatorFirstName = s.Facilitator!.FirstName,
                    FacilitatorLastName = s.Facilitator!.LastName,
                    SponsorshipId = sp.Id,
                    sp.SponsorId,
                    sp.CompanyId,
                    RecipientName = sp.CompanyId != null && sp.Company != null
                        ? sp.Company.Name
                        : sp.Sponsor != null
                            ? sp.Sponsor.FirstName + " " + sp.Sponsor.LastName
                            : string.Empty,
                    IsCompanySponsorship = sp.CompanyId != null,
                }))
            .OrderBy(x => x.FirstName)
            .ThenBy(x => x.LastName)
            .ThenBy(x => x.RecipientName)
            .ToListAsync();

        var letters = await db.Set<Letter>()
            .AsNoTracking()
            .Include(l => l.RejectedReason)
            .Where(l => l.PlanId == planId && l.ChapterId == chapterId)
            .ToListAsync();

        var currentLettersBySlot = letters
            .GroupBy(l => (l.StudentId, l.SponsorId, l.CompanyId))
            .ToDictionary(
                g => g.Key,
                g => g.FirstOrDefault(l => l.Status != DocumentStatus.Rejected)
                     ?? g.OrderByDescending(l => l.RejectedOn ?? l.UploadedOn).First());

        var rows = slots.Select(slot =>
        {
            exemptions.TryGetValue(slot.Id, out var exemptionReason);
            var isExempt = exemptionReason is not null;

            DocumentStatus? letterStatus = null;
            string? rejectionReasonDescription = null;
            string? rejectionNotes = null;
            DateTime? approvedOn = null;

            if (!isExempt
                && currentLettersBySlot.TryGetValue((slot.Id, slot.SponsorId, slot.CompanyId), out var letter))
            {
                letterStatus = letter.Status;
                rejectionReasonDescription = letter.RejectedReason?.Description;
                rejectionNotes = letter.RejectionNotes;
                approvedOn = letter.ApprovedOn;
            }

            return new LetterPlanProgressRowDataModel
            {
                StudentId = slot.Id,
                StudentFirstName = slot.FirstName,
                StudentLastName = slot.LastName,
                StudentNickName = slot.NickName,
                FacilitatorFirstName = slot.FacilitatorFirstName,
                FacilitatorLastName = slot.FacilitatorLastName,
                SponsorshipId = slot.SponsorshipId,
                SponsorId = slot.SponsorId,
                CompanyId = slot.CompanyId,
                RecipientName = slot.RecipientName,
                IsCompanySponsorship = slot.IsCompanySponsorship,
                IsExempt = isExempt,
                ExemptionReason = exemptionReason,
                LetterStatus = letterStatus,
                RejectionReasonDescription = rejectionReasonDescription,
                RejectionNotes = rejectionNotes,
                ApprovedOn = approvedOn,
            };
        }).ToList();

        return new LetterPlanProgressQueryResultDataModel
        {
            PlanStartsOn = plan.StartsOn,
            IsPlanCompleted = plan.Completed,
            Rows = rows,
        };
    }
}