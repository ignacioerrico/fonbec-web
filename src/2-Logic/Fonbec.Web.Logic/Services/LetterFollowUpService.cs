using Fonbec.Web.DataAccess.DataModels.LetterFollowUp;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models.LetterFollowUp;

namespace Fonbec.Web.Logic.Services;

public interface ILetterFollowUpService
{
    Task<LetterFollowUpViewModel> GetOpenTasksAsync(int managerChapterId);

    Task<bool> MarkTaskResolvedAsync(
        long assessmentId,
        LetterFollowUpTaskKind kind,
        int managerChapterId,
        int managerId);

    Task<bool> SetRedFlagPriorityAsync(
        long assessmentId,
        int managerChapterId,
        RedFlagPriority priority);
}

public sealed class LetterFollowUpService(
    ILetterFollowUpRepository repository,
    TimeProvider timeProvider) : ILetterFollowUpService
{
    public async Task<LetterFollowUpViewModel> GetOpenTasksAsync(int managerChapterId)
    {
        var result = await repository.GetOpenTasksAsync(managerChapterId);

        return new LetterFollowUpViewModel
        {
            RedFlags = result.RedFlags
                .OrderByDescending(task => task.Priority)
                .ThenByDescending(task => task.ReportedOn)
                .ThenBy(task => task.AssessmentId)
                .Select(task => Map(task, LetterFollowUpTaskKind.RedFlag))
                .ToList(),
            GreenFlags = result.GreenFlags
                .OrderByDescending(task => task.ReportedOn)
                .ThenBy(task => task.AssessmentId)
                .Select(task => Map(task, LetterFollowUpTaskKind.GreenFlag))
                .ToList(),
        };
    }

    public Task<bool> MarkTaskResolvedAsync(
        long assessmentId,
        LetterFollowUpTaskKind kind,
        int managerChapterId,
        int managerId)
    {
        var resolvedOn = timeProvider.GetUtcNow().UtcDateTime;

        return kind switch
        {
            LetterFollowUpTaskKind.RedFlag => repository.ResolveRedFlagAsync(
                assessmentId, managerChapterId, managerId, resolvedOn),
            LetterFollowUpTaskKind.GreenFlag => repository.ResolveGreenFlagAsync(
                assessmentId, managerChapterId, managerId, resolvedOn),
            _ => Task.FromResult(false),
        };
    }

    public Task<bool> SetRedFlagPriorityAsync(
        long assessmentId,
        int managerChapterId,
        RedFlagPriority priority)
    {
        if (!Enum.IsDefined(priority))
        {
            return Task.FromResult(false);
        }

        return repository.SetRedFlagPriorityAsync(
            assessmentId, managerChapterId, priority);
    }

    private static LetterFollowUpTaskViewModel Map(
        LetterFollowUpTaskDataModel task,
        LetterFollowUpTaskKind kind) =>
        new()
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