namespace Fonbec.Web.Logic.Models.RecipientMessages;

public static class PendingMessageBadge
{
    public static string? Format(int count) => count switch
    {
        > 10 => "10+",
        > 0 => count.ToString(),
        _ => null,
    };
}