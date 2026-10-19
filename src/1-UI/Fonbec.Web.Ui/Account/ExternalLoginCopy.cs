using Google.Apis.Auth.AspNetCore3;
using Microsoft.AspNetCore.Authentication.Facebook;

namespace Fonbec.Web.Ui.Account;

public static class ExternalLoginCopy
{
    public const string NotProvisioned =
        "Error: No existe un usuario de FONBEC con este correo. Pedile a un administrador que cree la cuenta.";

    public const string Cancelled =
        "Error: Cancelaste el inicio de sesión con Google.";

    public const string FacebookCancelled =
        "Error: Cancelaste el inicio de sesión con Facebook. Podés intentarlo de nuevo o ingresar con tu correo y contraseña.";

    public const string ProviderFailed =
        "Error: No se pudo completar el inicio de sesión con Google. Intentá de nuevo.";

    public const string FacebookProviderFailed =
        "Error: No se pudo completar el inicio de sesión con Facebook. Intentá de nuevo.";

    public const string LoadFailed =
        "Error: No se pudo cargar la información de inicio de sesión externo.";

    public const string LinkFailed =
        "Error: No se pudo vincular el inicio de sesión externo con tu cuenta. Contactá a un administrador.";

    public const string EmailMismatch =
        "Error: El correo del servicio externo no coincide con el de tu cuenta de FONBEC.";

    public const string NotAllowed =
        "Error: Esta cuenta no puede iniciar sesión. Contactá a un administrador.";

    public const string GoogleMissingEmail =
        "Error: Google no envió un correo. Revisá los permisos de la cuenta o iniciá sesión con tu contraseña.";

    public const string FacebookMissingEmail =
        "Error: Facebook no envió un correo. Revisá que la cuenta de Facebook tenga un correo y que hayas aceptado ese permiso, o iniciá sesión con tu contraseña.";

    public const string GoogleSignInButton = "Iniciar sesión con Google";

    public const string GoogleLinkButton = "Vincular Google";

    public const string FacebookSignInButton = "Iniciar sesión con Facebook";

    public const string FacebookLinkButton = "Vincular Facebook";

    public static bool IsGoogle(string? schemeName) =>
        schemeName == GoogleOpenIdConnectDefaults.AuthenticationScheme;

    public static bool IsFacebook(string? schemeName) =>
        schemeName == FacebookDefaults.AuthenticationScheme;

    public static string RemoteErrorMessage(string? remoteError, string? provider = null)
    {
        var cancelled = remoteError is not null
                        && remoteError.Contains("access_denied", StringComparison.OrdinalIgnoreCase);
        if (IsFacebook(provider))
        {
            return cancelled ? FacebookCancelled : FacebookProviderFailed;
        }

        if (IsGoogle(provider))
        {
            return cancelled ? Cancelled : ProviderFailed;
        }

        return cancelled
            ? "Error: Cancelaste el inicio de sesión externo."
            : "Error: No se pudo completar el inicio de sesión externo. Intentá de nuevo.";
    }

    public static string MissingEmailMessage(string? provider)
    {
        if (IsFacebook(provider))
        {
            return FacebookMissingEmail;
        }

        if (IsGoogle(provider))
        {
            return GoogleMissingEmail;
        }

        return "Error: El servicio externo no envió un correo. Revisá los permisos o iniciá sesión con tu contraseña.";
    }

    public static string SignInButtonText(string schemeName, string? displayName)
    {
        if (IsGoogle(schemeName))
        {
            return GoogleSignInButton;
        }

        if (IsFacebook(schemeName))
        {
            return FacebookSignInButton;
        }

        return displayName ?? schemeName;
    }

    public static string LinkButtonText(string schemeName, string? displayName)
    {
        if (IsGoogle(schemeName))
        {
            return GoogleLinkButton;
        }

        if (IsFacebook(schemeName))
        {
            return FacebookLinkButton;
        }

        return displayName ?? schemeName;
    }
}