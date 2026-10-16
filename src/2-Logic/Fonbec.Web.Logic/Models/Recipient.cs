namespace Fonbec.Web.Logic.Models;

public class Recipient
{
    public Recipient(string emailAddress, string? displayName = null)
    {
        DisplayName = $"\"{(displayName ?? emailAddress).Replace("\"", string.Empty)}\"";
        EmailAddress = $"<{emailAddress}>";
    }

    public string DisplayName { get; }

    public string EmailAddress { get; }
}