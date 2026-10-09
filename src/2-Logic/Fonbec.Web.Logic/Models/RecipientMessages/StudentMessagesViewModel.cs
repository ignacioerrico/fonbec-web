namespace Fonbec.Web.Logic.Models.RecipientMessages;

public sealed class StudentMessagesViewModel
{
    public List<StudentMessageItemViewModel> Pending { get; init; } = [];

    public List<StudentMessageItemViewModel> Delivered { get; init; } = [];
}

public sealed class StudentMessageItemViewModel
{
    public long RecipientMessageId { get; init; }

    public string StudentFullName { get; init; } = string.Empty;

    /// <summary>Padrino or Empresa.</summary>
    public string SenderKindLabel { get; init; } = string.Empty;

    /// <summary>Header, a blank line, then the message. Ready for the clipboard.</summary>
    public string CopyText { get; init; } = string.Empty;

    public string SenderName { get; init; } = string.Empty;

    public string Body { get; init; } = string.Empty;

    public DateTime SentOn { get; init; }

    public string SentOnLabel { get; init; } = string.Empty;

    public DateTime? SharedOn { get; init; }

    public string? SharedOnLabel { get; init; }

    public string? SharedByFullName { get; init; }
}