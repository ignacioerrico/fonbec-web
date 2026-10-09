using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.DataModels.RecipientMessages;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models.RecipientMessages;

namespace Fonbec.Web.Logic.Services;

public interface IRecipientMessageService
{
    /// <summary>
    /// Pending and delivered messages for the actor. A mediador sees students they currently
    /// facilitate. A coordinador sees students in their chapter. Role and chapter are read
    /// from the user record.
    /// </summary>
    Task<StudentMessagesViewModel> GetForActorAsync(int actorUserId);

    /// <summary>
    /// Pending messages for the actor, using the same scope as <see cref="GetForActorAsync"/>.
    /// </summary>
    Task<int> CountPendingForActorAsync(int actorUserId);

    /// <summary>
    /// Records that <paramref name="actorUserId"/> shared the message. An already-shared
    /// message keeps its original who and when. Returns false when the message is missing
    /// or outside the actor's scope. Does not send email.
    /// </summary>
    Task<bool> MarkSharedAsync(long recipientMessageId, int actorUserId);

    /// <summary>
    /// Clears the shared mark. An already-pending message stays pending. Returns false when
    /// the message is missing or outside the actor's scope. Does not send email.
    /// </summary>
    Task<bool> UndoSharedAsync(long recipientMessageId, int actorUserId);
}

public sealed class RecipientMessageService(
    IRecipientMessageRepository repository,
    TimeProvider timeProvider) : IRecipientMessageService
{
    public async Task<StudentMessagesViewModel> GetForActorAsync(int actorUserId)
    {
        var scope = await ResolveScopeAsync(actorUserId);
        if (scope is null)
        {
            return new StudentMessagesViewModel();
        }

        var rows = scope switch
        {
            RecipientMessageScope.Facilitator facilitator =>
                await repository.GetForFacilitatorAsync(facilitator.UserId),
            RecipientMessageScope.Chapter chapter =>
                await repository.GetForChapterAsync(chapter.ChapterId),
            _ => [],
        };

        return new StudentMessagesViewModel
        {
            Pending = rows
                .Where(row => row.SharedOn is null)
                .OrderBy(row => row.SentOn)
                .ThenBy(row => row.RecipientMessageId)
                .Select(Map)
                .ToList(),
            Delivered = rows
                .Where(row => row.SharedOn is not null)
                .OrderByDescending(row => row.SharedOn)
                .ThenByDescending(row => row.RecipientMessageId)
                .Select(Map)
                .ToList(),
        };
    }

    public async Task<int> CountPendingForActorAsync(int actorUserId)
    {
        var scope = await ResolveScopeAsync(actorUserId);
        return scope switch
        {
            RecipientMessageScope.Facilitator facilitator =>
                await repository.CountPendingForFacilitatorAsync(facilitator.UserId),
            RecipientMessageScope.Chapter chapter =>
                await repository.CountPendingForChapterAsync(chapter.ChapterId),
            _ => 0,
        };
    }

    public async Task<bool> MarkSharedAsync(long recipientMessageId, int actorUserId)
    {
        var scope = await ResolveScopeAsync(actorUserId);
        if (scope is null)
        {
            return false;
        }

        var state = await repository.GetInScopeAsync(recipientMessageId, scope);
        if (state is null)
        {
            return false;
        }

        if (state.SharedOn is not null)
        {
            return true;
        }

        var sharedOnUtc = timeProvider.GetUtcNow().UtcDateTime;
        return await repository.SetSharedAsync(recipientMessageId, scope, actorUserId, sharedOnUtc);
    }

    public async Task<bool> UndoSharedAsync(long recipientMessageId, int actorUserId)
    {
        var scope = await ResolveScopeAsync(actorUserId);
        if (scope is null)
        {
            return false;
        }

        var state = await repository.GetInScopeAsync(recipientMessageId, scope);
        if (state is null)
        {
            return false;
        }

        if (state.SharedOn is null && state.SharedById is null)
        {
            return true;
        }

        return await repository.ClearSharedAsync(recipientMessageId, scope);
    }

    private async Task<RecipientMessageScope?> ResolveScopeAsync(int actorUserId)
    {
        var actor = await repository.GetActorAsync(actorUserId);
        if (string.IsNullOrEmpty(actor?.Role))
        {
            return null;
        }

        if (actor.Role == FonbecRole.Uploader)
        {
            return new RecipientMessageScope.Facilitator(actorUserId);
        }

        if (actor.Role == FonbecRole.Manager && actor.ChapterId is int chapterId)
        {
            return new RecipientMessageScope.Chapter(chapterId);
        }

        return null;
    }

    private static StudentMessageItemViewModel Map(StudentMessageQueueItemDataModel row) =>
        new()
        {
            RecipientMessageId = row.RecipientMessageId,
            StudentFullName = row.StudentFullName,
            SenderKindLabel = row.IsCompany ? "Empresa" : "Padrino",
            SenderName = row.SenderName,
            CopyText = StudentMessageCopy.Format(row),
            Body = row.Body,
            SentOn = row.SentOn,
            SentOnLabel = row.SentOn.ToLocalTime().ToSpanishShortDate(),
            SharedOn = row.SharedOn,
            SharedOnLabel = row.SharedOn?.ToLocalTime().ToSpanishShortDate(),
            SharedByFullName = row.SharedByFullName,
        };
}