using Fonbec.Web.DataAccess.Entities.Enums;

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

    public string StudentFullName { get; init; } = string.Empty;

    public Gender StudentGender { get; init; }

    public bool IsCompany { get; init; }

    /// <summary>Null when the sender is a company.</summary>
    public Gender? SenderGender { get; init; }

    public string SenderName { get; init; } = string.Empty;

    public string Body { get; init; } = string.Empty;

    public DateTime SentOn { get; init; }

    public DateTime? SharedOn { get; init; }

    public string? SharedByFullName { get; init; }
}

public class RecipientMessageShareStateDataModel
{
    public DateTime? SharedOn { get; init; }

    public int? SharedById { get; init; }
}

public class RecipientMessageActorDataModel
{
    public string Role { get; init; } = string.Empty;

    public int? ChapterId { get; init; }
}

/// <summary>
/// What the facilitator (mediador) email needs. The chapter manager (coordinador) is not included.
/// </summary>
public class RecipientMessageFacilitatorNotificationDataModel
{
    public DateTime? FacilitatorNotifiedOn { get; init; }

    public string? FacilitatorEmail { get; init; }

    public string StudentFullName { get; init; } = string.Empty;

    public string StudentFirstName { get; init; } = string.Empty;

    public string? StudentNickName { get; init; }

    public Gender StudentGender { get; init; }

    public bool IsCompany { get; init; }

    /// <summary>Null when the sender is a company.</summary>
    public Gender? SenderGender { get; init; }

    public string SenderName { get; init; } = string.Empty;

    public string Body { get; init; } = string.Empty;
}