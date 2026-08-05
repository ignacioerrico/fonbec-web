using Fonbec.Web.DataAccess.DataModels.LetterPlanProgress;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models.LetterPlanProgress;

namespace Fonbec.Web.Logic.Services;

public interface ILetterPlanProgressService
{
    Task<LetterPlanProgressViewModel?> GetProgressAsync(int planId, int managerChapterId);

    Task<bool> ExemptStudentAsync(
        int planId, int studentId, int managerChapterId, int managerUserId, string reason);

    Task<bool> RevokeExemptionAsync(
        int planId, int studentId, int managerChapterId, int managerUserId);

    /// <summary>
    /// Marks the plan completed when every letter slot is approved or the student is exempt
    /// (no in-progress or missing/rejected slots remain).
    /// </summary>
    Task<bool> TryCompletePlanIfDoneAsync(int planId, int chapterId);
}

public class LetterPlanProgressService(
    ILetterPlanProgressRepository letterPlanProgressRepository,
    ILetterExemptionRepository letterExemptionRepository,
    IPlannedDeliveryRepository plannedDeliveryRepository,
    IStudentRepository studentRepository,
    TimeProvider timeProvider) : ILetterPlanProgressService
{
    public async Task<LetterPlanProgressViewModel?> GetProgressAsync(int planId, int managerChapterId)
    {
        var result = await letterPlanProgressRepository.GetProgressAsync(planId, managerChapterId);
        if (result is null)
        {
            return null;
        }

        return MapToViewModel(result);
    }

    public async Task<bool> ExemptStudentAsync(
        int planId, int studentId, int managerChapterId, int managerUserId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return false;
        }

        var studentChapterId = await studentRepository.GetStudentChapterIdAsync(studentId);
        if (studentChapterId != managerChapterId)
        {
            return false;
        }

        var progress = await letterPlanProgressRepository.GetProgressAsync(planId, managerChapterId);
        if (progress is null)
        {
            return false;
        }

        if (progress.IsPlanCompleted)
        {
            return false;
        }

        var created = await letterExemptionRepository.CreateExemptionAsync(
            studentId,
            planId,
            managerChapterId,
            reason.Trim(),
            managerUserId,
            timeProvider.GetUtcNow().UtcDateTime);

        if (created)
        {
            await TryCompletePlanIfDoneAsync(planId, managerChapterId);
        }

        return created;
    }

    public async Task<bool> RevokeExemptionAsync(
        int planId, int studentId, int managerChapterId, int managerUserId)
    {
        var studentChapterId = await studentRepository.GetStudentChapterIdAsync(studentId);
        if (studentChapterId != managerChapterId)
        {
            return false;
        }

        var progress = await letterPlanProgressRepository.GetProgressAsync(planId, managerChapterId);
        if (progress is null)
        {
            return false;
        }

        if (progress.IsPlanCompleted)
        {
            return false;
        }

        return await letterExemptionRepository.RevokeExemptionAsync(
            studentId,
            planId,
            managerUserId,
            timeProvider.GetUtcNow().UtcDateTime);
    }

    public async Task<bool> TryCompletePlanIfDoneAsync(int planId, int chapterId)
    {
        var result = await letterPlanProgressRepository.GetProgressAsync(planId, chapterId);
        if (result is null || result.IsPlanCompleted)
        {
            return false;
        }

        var viewModel = MapToViewModel(result);
        if (viewModel.Summary.InProgress == 0 && viewModel.Summary.MissingOrRejected == 0)
        {
            return await plannedDeliveryRepository.MarkPlanCompletedAsync(planId);
        }

        return false;
    }

    private static LetterPlanProgressViewModel MapToViewModel(LetterPlanProgressQueryResultDataModel result)
    {
        var rows = result.Rows.Select(row =>
        {
            var status = LetterPlanDisplayStatusExtensions.FromRow(row.IsExempt, row.LetterStatus);
            return new LetterPlanProgressRowViewModel
            {
                StudentId = row.StudentId,
                StudentFirstName = row.StudentFirstName,
                StudentLastName = row.StudentLastName,
                StudentNickName = row.StudentNickName,
                FacilitatorFullName = $"{row.FacilitatorFirstName} {row.FacilitatorLastName}".Trim(),
                SponsorshipId = row.SponsorshipId,
                SponsorId = row.SponsorId,
                CompanyId = row.CompanyId,
                RecipientName = row.RecipientName,
                IsCompanySponsorship = row.IsCompanySponsorship,
                Status = status,
                StatusLabel = status.ToStatusLabel(),
                RejectionReason = BuildRejectionReason(row.RejectionReasonDescription, row.RejectionNotes),
                ExemptionReason = row.ExemptionReason,
                ApprovedOn = row.ApprovedOn,
                IsStudentExempt = row.IsExempt,
            };
        }).ToList();

        var seenStudents = new HashSet<int>();
        foreach (var row in rows)
        {
            if (seenStudents.Add(row.StudentId))
            {
                row.IsFirstRowForStudent = true;
            }
        }

        var requiredRows = rows.Where(r => r.Status != LetterPlanDisplayStatus.Exempt).ToList();

        var totalRequired = requiredRows.Count;
        var approved = requiredRows.Count(r => r.Status.CountsAsApproved());
        var inProgress = requiredRows.Count(r => r.Status.CountsAsInProgress());
        var missingOrRejected = requiredRows.Count(r => r.Status.CountsAsMissingOrRejected());

        var completionPercent = totalRequired == 0
            ? 0m
            : Math.Round(100m * approved / totalRequired, 0);

        return new LetterPlanProgressViewModel
        {
            PlanLabel = LetterPlanProgressFormatting.FormatPlanLabel(result.PlanStartsOn),
            IsPlanCompleted = result.IsPlanCompleted,
            Summary = new LetterPlanProgressSummaryViewModel
            {
                TotalRequired = totalRequired,
                Approved = approved,
                InProgress = inProgress,
                MissingOrRejected = missingOrRejected,
                CompletionPercent = completionPercent,
            },
            Rows = rows,
        };
    }

    private static string? BuildRejectionReason(string? reasonDescription, string? rejectionNotes)
    {
        if (string.IsNullOrWhiteSpace(reasonDescription))
        {
            return string.IsNullOrWhiteSpace(rejectionNotes) ? null : rejectionNotes.Trim();
        }

        if (string.IsNullOrWhiteSpace(rejectionNotes))
        {
            return reasonDescription;
        }

        return $"{reasonDescription}: {rejectionNotes.Trim()}";
    }
}