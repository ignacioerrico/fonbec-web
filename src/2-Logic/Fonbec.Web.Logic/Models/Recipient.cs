namespace Fonbec.Web.Logic.Models;

public class Recipient
{
    public Recipient(string emailAddress, string? displayName = null)
    {
        var displayNameToUse = displayName ?? emailAddress;
        DisplayName = displayNameToUse.Contains(' ')
            ? $"\"{displayNameToUse}\""
            : displayNameToUse;
        EmailAddress = $"<{emailAddress}>";
    }

    public string DisplayName { get; }

    public string EmailAddress { get; }
}