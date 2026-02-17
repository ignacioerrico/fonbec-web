using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.Authorization;

namespace Fonbec.Web.Ui.Authorization;

/// <summary>
/// Define custom access.
/// This is a complement to the page access.
/// (So you can define access to pages with the PageMetadataAttribute and in this class you can define access to more granular elements.)
/// </summary>
public static class CustomAccess
{
    public const string ResetPassword = "ResetPassword";

    public static readonly List<PageAccessInfo> List =
    [
        new(ResetPassword, "Restablecer contraseña de usuario", [FonbecRole.Admin, FonbecRole.Manager]),
    ];
}
