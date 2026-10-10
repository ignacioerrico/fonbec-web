using Fonbec.Web.DataAccess.DataModels.RecipientMessages;
using Fonbec.Web.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Repositories;

public interface IRecipientMessageRepository
{
    /// <summary>
    /// Messages for students whose current facilitator is <paramref name="facilitatorUserId"/>.
    /// A facilitator change moves the message with the student.
    /// </summary>
    Task<List<StudentMessageQueueItemDataModel>> GetForFacilitatorAsync(int facilitatorUserId);

    Task<List<StudentMessageQueueItemDataModel>> GetForChapterAsync(int chapterId);

    Task<int> CountPendingForFacilitatorAsync(int facilitatorUserId);

    Task<int> CountPendingForChapterAsync(int chapterId);

    /// <summary>
    /// Role and chapter from the user record, on a context that is not shared with Identity.
    /// </summary>
    Task<RecipientMessageActorDataModel?> GetActorAsync(int userId);

    Task<RecipientMessageShareStateDataModel?> GetInScopeAsync(long recipientMessageId, RecipientMessageScope scope);

    /// <summary>
    /// Sets who shared the message and when. Already-shared rows are left unchanged.
    /// Returns false when the message is missing or outside <paramref name="scope"/>.
    /// </summary>
    Task<bool> SetSharedAsync(long recipientMessageId, RecipientMessageScope scope, int actorUserId, DateTime sharedOnUtc);

    /// <summary>
    /// Clears who shared the message and when. Already-pending rows are left unchanged.
    /// Returns false when the message is missing or outside <paramref name="scope"/>.
    /// </summary>
    Task<bool> ClearSharedAsync(long recipientMessageId, RecipientMessageScope scope);

    /// <summary>
    /// Current facilitator email, student name, and sender name for one saved message.
    /// Null when the message does not exist.
    /// </summary>
    Task<RecipientMessageFacilitatorNotificationDataModel?> GetFacilitatorNotificationAsync(long recipientMessageId);

    /// <summary>
    /// Sets <see cref="RecipientMessage.FacilitatorNotifiedOn"/> when it is still null.
    /// A later facilitator change does not clear it.
    /// </summary>
    Task MarkFacilitatorNotifiedAsync(long recipientMessageId, DateTime notifiedOnUtc);
}

public class RecipientMessageRepository(IDbContextFactory<FonbecWebDbContext> dbContext) : IRecipientMessageRepository
{
    public Task<List<StudentMessageQueueItemDataModel>> GetForFacilitatorAsync(int facilitatorUserId) =>
        QueryAsync(messages => messages.Where(m => m.Student.FacilitatorId == facilitatorUserId));

    public Task<List<StudentMessageQueueItemDataModel>> GetForChapterAsync(int chapterId) =>
        QueryAsync(messages => messages.Where(m => m.Student.ChapterId == chapterId));

    public Task<int> CountPendingForFacilitatorAsync(int facilitatorUserId) =>
        CountPendingAsync(messages => messages.Where(m =>
            m.Student.FacilitatorId == facilitatorUserId && m.SharedOn == null));

    public Task<int> CountPendingForChapterAsync(int chapterId) =>
        CountPendingAsync(messages => messages.Where(m =>
            m.Student.ChapterId == chapterId && m.SharedOn == null));

    public async Task<RecipientMessageActorDataModel?> GetActorAsync(int userId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var user = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.ChapterId })
            .SingleOrDefaultAsync();

        if (user is null)
        {
            return null;
        }

        var role = await db.UserRoles
            .AsNoTracking()
            .Where(userRole => userRole.UserId == userId)
            .Join(
                db.Roles.AsNoTracking(),
                userRole => userRole.RoleId,
                roleRow => roleRow.Id,
                (_, roleRow) => roleRow.Name)
            .SingleOrDefaultAsync();

        return new RecipientMessageActorDataModel
        {
            Role = role ?? "",
            ChapterId = user.ChapterId,
        };
    }

    public async Task<RecipientMessageShareStateDataModel?> GetInScopeAsync(
        long recipientMessageId, RecipientMessageScope scope)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        return await InScope(db.RecipientMessages.AsNoTracking(), scope)
            .Where(m => m.RecipientMessageId == recipientMessageId)
            .Select(m => new RecipientMessageShareStateDataModel
            {
                SharedOn = m.SharedOn,
                SharedById = m.SharedById,
            })
            .SingleOrDefaultAsync();
    }

    public async Task<bool> SetSharedAsync(
        long recipientMessageId, RecipientMessageScope scope, int actorUserId, DateTime sharedOnUtc)
    {
        await using var db = await dbContext.CreateDbContextAsync();
        var message = await InScope(db.RecipientMessages, scope)
            .SingleOrDefaultAsync(m => m.RecipientMessageId == recipientMessageId);

        if (message is null)
        {
            return false;
        }

        if (message.SharedOn is not null)
        {
            return true;
        }

        message.SharedOn = sharedOnUtc;
        message.SharedById = actorUserId;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ClearSharedAsync(long recipientMessageId, RecipientMessageScope scope)
    {
        await using var db = await dbContext.CreateDbContextAsync();
        var message = await InScope(db.RecipientMessages, scope)
            .SingleOrDefaultAsync(m => m.RecipientMessageId == recipientMessageId);

        if (message is null)
        {
            return false;
        }

        if (message.SharedOn is null && message.SharedById is null)
        {
            return true;
        }

        message.SharedOn = null;
        message.SharedById = null;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<RecipientMessageFacilitatorNotificationDataModel?> GetFacilitatorNotificationAsync(
        long recipientMessageId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        return await db.RecipientMessages
            .AsNoTracking()
            .Where(m => m.RecipientMessageId == recipientMessageId)
            .Select(m => new RecipientMessageFacilitatorNotificationDataModel
            {
                FacilitatorNotifiedOn = m.FacilitatorNotifiedOn,
                FacilitatorEmail = m.Student.Facilitator.Email,
                StudentFullName = m.Student.FirstName + " " + m.Student.LastName,
                StudentFirstName = m.Student.FirstName,
                StudentNickName = m.Student.NickName,
                StudentGender = m.Student.Gender,
                IsCompany = m.CompanyId != null,
                SenderGender = m.CompanyId != null ? null : m.Sponsor!.Gender,
                SenderName = m.CompanyId != null
                    ? m.Company!.Name
                    : m.Sponsor!.FirstName + " " + m.Sponsor.LastName,
                Body = m.Body,
            })
            .SingleOrDefaultAsync();
    }

    public async Task MarkFacilitatorNotifiedAsync(long recipientMessageId, DateTime notifiedOnUtc)
    {
        await using var db = await dbContext.CreateDbContextAsync();
        var message = await db.RecipientMessages
            .SingleOrDefaultAsync(m => m.RecipientMessageId == recipientMessageId);

        if (message is null || message.FacilitatorNotifiedOn is not null)
        {
            return;
        }

        message.FacilitatorNotifiedOn = notifiedOnUtc;
        await db.SaveChangesAsync();
    }

    private async Task<int> CountPendingAsync(
        Func<IQueryable<RecipientMessage>, IQueryable<RecipientMessage>> filter)
    {
        await using var db = await dbContext.CreateDbContextAsync();
        return await filter(db.RecipientMessages.AsNoTracking()).CountAsync();
    }

    private async Task<List<StudentMessageQueueItemDataModel>> QueryAsync(
        Func<IQueryable<RecipientMessage>, IQueryable<RecipientMessage>> filter)
    {
        await using var db = await dbContext.CreateDbContextAsync();
        var messages = filter(db.RecipientMessages.AsNoTracking());

        return await messages
            .Select(m => new StudentMessageQueueItemDataModel
            {
                RecipientMessageId = m.RecipientMessageId,
                StudentFullName = m.Student.FirstName + " " + m.Student.LastName,
                StudentGender = m.Student.Gender,
                IsCompany = m.CompanyId != null,
                SenderGender = m.CompanyId != null ? null : m.Sponsor!.Gender,
                SenderName = m.CompanyId != null
                    ? m.Company!.Name
                    : m.Sponsor!.FirstName + " " + m.Sponsor.LastName,
                Body = m.Body,
                SentOn = m.SentOn,
                SharedOn = m.SharedOn,
                SharedByFullName = m.SharedById == null
                    ? null
                    : m.SharedBy!.FirstName + " " + m.SharedBy.LastName,
            })
            .ToListAsync();
    }

    private static IQueryable<RecipientMessage> InScope(
        IQueryable<RecipientMessage> messages, RecipientMessageScope scope) =>
        scope switch
        {
            RecipientMessageScope.Facilitator facilitator =>
                messages.Where(m => m.Student.FacilitatorId == facilitator.UserId),
            RecipientMessageScope.Chapter chapter =>
                messages.Where(m => m.Student.ChapterId == chapter.ChapterId),
            _ => throw new ArgumentOutOfRangeException(nameof(scope)),
        };
}