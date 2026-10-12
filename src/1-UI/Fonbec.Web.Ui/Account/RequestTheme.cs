namespace Fonbec.Web.Ui.Account;

/// <summary>
/// Theme read from the request cookie before the page renders.
/// Static layouts use this because they do not receive <see cref="HttpContext"/> as a cascading parameter.
/// </summary>
public sealed class RequestTheme
{
    public bool IsDark { get; set; }
}