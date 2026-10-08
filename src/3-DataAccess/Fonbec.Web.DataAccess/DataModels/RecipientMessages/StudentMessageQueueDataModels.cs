namespace Fonbec.Web.DataAccess.DataModels.RecipientMessages;

/// <summary>
/// Whose students a staff member may see. Resolved on the server from the actor's user record.
/// </summary>
public abstract record RecipientMessageScope
{
    public sealed record Facilitator(int UserId) : RecipientMessageScope;

    public sealed record Chapter(int ChapterId) : RecipientMessageScope;
}

public class StudentMessageQueueItemDataModel
{
    public long RecipientMessageId { get; init; }

    public string StudentFullName { get; init; } = "";

    public bool IsCompany { get; init; }

    public string SenderName { get; init; } = "";

    public string Body { get; init; } = "";

    public DateTime SentOn { get; init; }

    public DateTime? SharedOn { get; init; }

    public string? SharedByFullName { get; init; }
}

public class RecipientMessageShareStateDataModel
{
    public DateTime? SharedOn { get; init; }

    public int? SharedById { get; init; }
}