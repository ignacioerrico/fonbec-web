using System.Security.Claims;

namespace Fonbec.Web.Ui.Account;

public enum ExternalLoginDecision
{
    SignIn,
    Lockout,
    NotAllowed,
    NotProvisioned,
    LinkAndSignIn
}

/// <summary>
/// Decides what an external sign-in may do. There is no outcome that creates a user.
/// </summary>
public static class ExternalLoginPolicy
{
    /// <summary>
    /// Same normalization as user creation: trim, then lowercase.
    /// </summary>
    public static string? NormalizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        return email.Trim().ToLower();
    }

    public static string? ReadEmail(ClaimsPrincipal principal)
    {
        var email = principal.FindFirstValue(ClaimTypes.Email)
                    ?? principal.FindFirstValue("email");
        return NormalizeEmail(email);
    }

    public static bool EmailsMatch(string? accountEmail, string? externalEmail)
    {
        var account = NormalizeEmail(accountEmail);
        var external = NormalizeEmail(externalEmail);
        return account is not null && account == external;
    }

    public static ExternalLoginDecision Decide(
        bool alreadySignedIn,
        bool alreadyLinkedAndLockedOut,
        bool alreadyLinkedAndNotAllowed,
        bool matchingUserExists,
        bool matchingUserLockedOut)
    {
        if (alreadySignedIn)
        {
            return ExternalLoginDecision.SignIn;
        }

        // Lockout is checked before a new provider login is stored.
        if (alreadyLinkedAndLockedOut || (matchingUserExists && matchingUserLockedOut))
        {
            return ExternalLoginDecision.Lockout;
        }

        if (alreadyLinkedAndNotAllowed)
        {
            return ExternalLoginDecision.NotAllowed;
        }

        if (!matchingUserExists)
        {
            return ExternalLoginDecision.NotProvisioned;
        }

        return ExternalLoginDecision.LinkAndSignIn;
    }
}