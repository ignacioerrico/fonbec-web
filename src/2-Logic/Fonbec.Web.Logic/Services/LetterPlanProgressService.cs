using Fonbec.Web.DataAccess.DataModels.LetterPlanProgress;
using Fonbec.Web.DataAccess.DataModels.LetterFollowUp;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models.LetterPlanProgress;
using Fonbec.Web.Logic.Models.LetterFollowUp;

namespace Fonbec.Web.Logic.Services;

public interface ILetterPlanProgressService
{
    Task<LetterPlanProgressViewModel?> GetProgressAsync(int planId, int managerChapterId);

    Task<bool> ExemptStudentAsync(
        int planId, int studentId, int managerChapterId, int managerUserId, string reason);

    Task<bool> RevokeExemptionAsync(
        int planId, int studentId, int managerChapterId, int managerUserId);
}

public class LetterPlanProgressService(
    ILetterPlanProgressRepository letterPlanProgressRepository,
    ILetterExemptionRepository letterExemptionRepository,
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

        var studentRows = progress.Rows
            .Where(row => row.StudentId == studentId)
            .ToList();

        // Exemption is an alternative to the student's entire letter obligation for this plan: it is
        // all or nothing. Once a letter stands for any sponsor, the student must complete the rest and
        // can no longer be exempted. A rejected letter still has to be provided, so it does not count.
        if (studentRows.Count == 0 || studentRows.Any(row => row.LetterStatus is not null and not DocumentStatus.Rejected))
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

        // Plan chapter scoping: progress is null when the plan is outside the manager's chapter.
        var progress = await letterPlanProgressRepository.GetProgressAsync(planId, managerChapterId);
        if (progress is null || progress.IsPlanCompleted)
        {
            return false;
        }

        var revoked = await letterExemptionRepository.RevokeExemptionAsync(
            studentId,
            planId,
            managerUserId,
            timeProvider.GetUtcNow().UtcDateTime);

        return revoked;
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
                RedFlag = MapFlag(row.RedFlag, LetterFollowUpTaskKind.RedFlag),
                GreenFlag = MapFlag(row.GreenFlag, LetterFollowUpTaskKind.GreenFlag),
            };
        }).ToList();

        return new LetterPlanProgressViewModel
        {
            PlanLabel = LetterPlanProgressFormatting.FormatPlanLabel(result.PlanStartsOn),
            IsPlanCompleted = result.IsPlanCompleted,
            Summary = rows.Select(r => r.Status).ToSummary(),
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

    private static LetterFollowUpTaskViewModel? MapFlag(
        LetterFollowUpTaskDataModel? task,
        LetterFollowUpTaskKind kind) =>
        task is null
            ? null
            : new LetterFollowUpTaskViewModel
            {
                AssessmentId = task.AssessmentId,
                Kind = kind,
                StudentFullName = $"{task.StudentFirstName} {task.StudentLastName}".Trim(),
                FacilitatorFullName = $"{task.FacilitatorFirstName} {task.FacilitatorLastName}".Trim(),
                FacilitatorEmail = task.FacilitatorEmail,
                ReviewerFullName = $"{task.ReviewerFirstName} {task.ReviewerLastName}".Trim(),
                ReviewerEmail = task.ReviewerEmail,
                ReportedOn = task.ReportedOn,
                Comment = task.Comment,
                Priority = task.Priority,
            };
}