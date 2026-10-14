using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.DataModels.RecipientMessages;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models.RecipientMessages;

namespace Fonbec.Web.Logic.Services;

public interface IRecipientMessageService
{
    /// <summary>
    /// Pending messages and the first page of already-shared ones. A mediador sees students
    /// they currently facilitate. A coordinador sees students in their chapter. Role and
    /// chapter are read from the user record.
    /// </summary>
    Task<StudentMessagesViewModel> GetForActorAsync(int actorUserId);

    /// <summary>
    /// The next page of already-shared messages, using the same scope as
    /// <see cref="GetForActorAsync"/>. <paramref name="skip"/> is how many delivered
    /// messages the caller already has.
    /// </summary>
    Task<DeliveredStudentMessagesPage> GetMoreDeliveredAsync(int actorUserId, int skip);

    /// <summary>
    /// Pending messages for the actor, using the same scope as <see cref="GetForActorAsync"/>.
    /// </summary>
    Task<int> CountPendingForActorAsync(int actorUserId);

    /// <summary>
    /// Records that the student's mediador shared the message. Only a mediador can do this.
    /// A coordinador can see the message but cannot mark it. An already-shared message keeps
    /// its original who and when. Returns false when the actor is not a mediador, or the
    /// message is missing or outside their students. Does not send email.
    /// </summary>
    Task<bool> MarkSharedAsync(long recipientMessageId, int actorUserId);

    /// <summary>
    /// Clears the shared mark. Only a mediador can do this. An already-pending message stays
    /// pending. Returns false when the actor is not a mediador, or the message is missing or
    /// outside their students. Does not send email.
    /// </summary>
    Task<bool> UndoSharedAsync(long recipientMessageId, int actorUserId);
}

public sealed class RecipientMessageService(
    IRecipientMessageRepository repository,
    TimeProvider timeProvider) : IRecipientMessageService
{
    public const int DeliveredPageSize = 10;

    public async Task<StudentMessagesViewModel> GetForActorAsync(int actorUserId)
    {
        var scope = await ResolveScopeAsync(actorUserId);
        if (scope is null)
        {
            return new StudentMessagesViewModel();
        }

        var pending = await LoadPendingAsync(scope) ?? [];
        var delivered = await LoadDeliveredPageAsync(scope, skip: 0) ?? new StudentMessageQueuePageDataModel();

        return new StudentMessagesViewModel
        {
            Pending = pending.Select(Map).ToList(),
            Delivered = delivered.Items.Select(Map).ToList(),
            HasMoreDelivered = delivered.HasMore,
        };
    }

    public async Task<DeliveredStudentMessagesPage> GetMoreDeliveredAsync(int actorUserId, int skip)
    {
        var scope = await ResolveScopeAsync(actorUserId);
        if (scope is null)
        {
            return new DeliveredStudentMessagesPage();
        }

        var delivered = await LoadDeliveredPageAsync(scope, skip) ?? new StudentMessageQueuePageDataModel();
        return new DeliveredStudentMessagesPage
        {
            Items = delivered.Items.Select(Map).ToList(),
            HasMore = delivered.HasMore,
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
        if (scope is not RecipientMessageScope.Facilitator)
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
        if (scope is not RecipientMessageScope.Facilitator)
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

    private Task<List<StudentMessageQueueItemDataModel>> LoadPendingAsync(RecipientMessageScope scope) =>
        scope switch
        {
            RecipientMessageScope.Facilitator facilitator =>
                repository.GetPendingForFacilitatorAsync(facilitator.UserId),
            RecipientMessageScope.Chapter chapter =>
                repository.GetPendingForChapterAsync(chapter.ChapterId),
            _ => Task.FromResult(new List<StudentMessageQueueItemDataModel>()),
        };

    private Task<StudentMessageQueuePageDataModel> LoadDeliveredPageAsync(RecipientMessageScope scope, int skip) =>
        scope switch
        {
            RecipientMessageScope.Facilitator facilitator =>
                repository.GetDeliveredForFacilitatorAsync(facilitator.UserId, skip, DeliveredPageSize),
            RecipientMessageScope.Chapter chapter =>
                repository.GetDeliveredForChapterAsync(chapter.ChapterId, skip, DeliveredPageSize),
            _ => Task.FromResult(new StudentMessageQueuePageDataModel()),
        };

    private static StudentMessageItemViewModel Map(StudentMessageQueueItemDataModel row) =>
        new()
        {
            RecipientMessageId = row.RecipientMessageId,
            StudentFullName = row.StudentFullName,
            StudentRoleLabel = GenderWords.Becario(row.StudentGender),
            SenderKindLabel = row.IsCompany ? "empresa" : GenderWords.Padrino(row.SenderGender),
            FacilitatorFullName = row.FacilitatorFullName,
            FacilitatorEmail = row.FacilitatorEmail,
            SenderName = row.SenderName,
            CopyText = StudentMessageCopy.Format(row),
            Body = row.Body,
            SentOn = row.SentOn,
            SentOnLabel = row.SentOn.ToLocalTime().ToSpanishLongDate(),
            SharedOn = row.SharedOn,
            SharedOnLabel = row.SharedOn?.ToLocalTime().ToSpanishLongDate(),
            SharedByFullName = row.SharedByFullName,
        };
}