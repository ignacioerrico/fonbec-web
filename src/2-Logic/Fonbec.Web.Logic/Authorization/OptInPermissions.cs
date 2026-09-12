using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.Constants;

namespace Fonbec.Web.Logic.Authorization;

/// <summary>
/// Feature permissions that are opt-in (not granted by role default).
/// Kept out of page discovery / CustomAccess so they never appear on the Admin deny-list UI.
/// </summary>
public static class OptInPermissions
{
    public static readonly PageAccessInfo DigitalImprovement = new(
        DocumentPermission.DigitalImprovement,
        "Mejora digital de imágenes",
        [FonbecRole.Reviewer, FonbecRole.Manager]);

    public static readonly IReadOnlyList<PageAccessInfo> All = [DigitalImprovement];

    public static bool IsOptIn(string codename) =>
        All.Any(p => p.Codename == codename);
}