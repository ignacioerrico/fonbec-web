using System.Globalization;
using Fonbec.Web.DataAccess.DataModels.LetterPlanProgress;
using Fonbec.Web.DataAccess.DataModels.LetterFollowUp;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.ExtensionMethods;
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
    private static readonly CultureInfo EsAr = CultureInfo.GetCultureInfo("es-AR");
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
                ReviewEvaluation = MapReviewEvaluation(row),
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

    private static LetterReviewEvaluationViewModel? MapReviewEvaluation(LetterPlanProgressRowDataModel row)
    {
        if (row.LetterStatus != DocumentStatus.Approved
            || row.SpellingScore is not int spelling
            || row.PenmanshipScore is not int penmanship
            || row.ContentScore is not int content
            || row.ConfirmedWrittenDate is not DateTime written)
        {
            return null;
        }

        string? delay = null;
        if (row.AddresseeNotificationSentOn is DateTime notified)
        {
            var days = Math.Max(0, (notified.Date - written.Date).Days);
            delay = FormatCalendarDays(days);
        }

        return new LetterReviewEvaluationViewModel
        {
            WrittenOnText = written.ToSpanishShortDate(),
            NotificationSentOnText = row.AddresseeNotificationSentOn?.Date.ToSpanishShortDate(),
            DelayText = delay,
            QueueWaitText = FormatQueueWait(row),
            SpellingScore = spelling,
            PenmanshipScore = penmanship,
            ContentScore = content,
        };
    }

    private static string FormatQueueWait(LetterPlanProgressRowDataModel row)
    {
        var isImage = row.DigitalImprovementStatus != DigitalImprovementStatus.NotApplicable;
        var start = isImage ? row.ImprovementCompletedOn : row.UploadedOn;
        if (start is null || row.ReviewFirstLockedOn is null)
        {
            return "Sin dato";
        }

        var duration = row.ReviewFirstLockedOn.Value - start.Value;
        if (duration < TimeSpan.Zero)
        {
            duration = TimeSpan.Zero;
        }

        return FormatElapsed(duration);
    }

    private static string FormatCalendarDays(int days)
    {
        if (days < 1)
        {
            return "menos de 1 día";
        }

        if (days == 1)
        {
            return "1 día";
        }

        return $"{days.ToString("0.#", EsAr)} días";
    }

    private static string FormatElapsed(TimeSpan duration)
    {
        if (duration.TotalDays >= 1)
        {
            var days = duration.TotalDays;
            if (days < 1.05)
            {
                return "1 día";
            }

            return $"{days.ToString("0.#", EsAr)} días";
        }

        if (duration.TotalHours >= 1)
        {
            var hours = (int)duration.TotalHours;
            var minutes = duration.Minutes;
            return minutes == 0
                ? $"{hours} h"
                : $"{hours} h {minutes} min";
        }

        if (duration.TotalMinutes < 1)
        {
            return "menos de 1 min";
        }

        return $"{Math.Round(duration.TotalMinutes).ToString("0", EsAr)} min";
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