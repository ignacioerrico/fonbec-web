namespace Fonbec.Web.Logic.Models.RecipientMessages;

public sealed class StudentMessagesViewModel
{
    public List<StudentMessageItemViewModel> Pending { get; init; } = [];

    public List<StudentMessageItemViewModel> Delivered { get; init; } = [];
}

public sealed class StudentMessageItemViewModel
{
    public long RecipientMessageId { get; init; }

    public string StudentFullName { get; init; } = "";

    /// <summary>Padrino or Empresa.</summary>
    public string SenderKindLabel { get; init; } = "";

    public string SenderName { get; init; } = "";

    public string Body { get; init; } = "";

    public DateTime SentOn { get; init; }

    public string SentOnLabel { get; init; } = "";

    public DateTime? SharedOn { get; init; }

    public string? SharedOnLabel { get; init; }

    public string? SharedByFullName { get; init; }
}