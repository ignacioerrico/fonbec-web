using Google.Apis.Auth.AspNetCore3;

namespace Fonbec.Web.Ui.Account;

public static class ExternalLoginCopy
{
    public const string NotProvisioned =
        "Error: No existe un usuario de FONBEC con este correo. Pedile a un administrador que cree la cuenta.";

    public const string Cancelled =
        "Error: Cancelaste el inicio de sesión con Google.";

    public const string ProviderFailed =
        "Error: No se pudo completar el inicio de sesión con Google. Intentá de nuevo.";

    public const string LoadFailed =
        "Error: No se pudo cargar la información de inicio de sesión externo.";

    public const string LinkFailed =
        "Error: No se pudo vincular el inicio de sesión externo con tu cuenta. Contactá a un administrador.";

    public const string EmailMismatch =
        "Error: El correo del servicio externo no coincide con el de tu cuenta de FONBEC.";

    public const string NotAllowed =
        "Error: Esta cuenta no puede iniciar sesión. Contactá a un administrador.";

    public const string GoogleSignInButton = "Iniciar sesión con Google";

    public const string GoogleLinkButton = "Vincular Google";

    public static bool IsGoogle(string schemeName) =>
        schemeName == GoogleOpenIdConnectDefaults.AuthenticationScheme;

    public static string RemoteErrorMessage(string? remoteError) =>
        remoteError is not null && remoteError.Contains("access_denied", StringComparison.OrdinalIgnoreCase)
            ? Cancelled
            : ProviderFailed;

    public static string SignInButtonText(string schemeName, string? displayName) =>
        IsGoogle(schemeName) ? GoogleSignInButton : displayName ?? schemeName;

    public static string LinkButtonText(string schemeName, string? displayName) =>
        IsGoogle(schemeName) ? GoogleLinkButton : displayName ?? schemeName;
}