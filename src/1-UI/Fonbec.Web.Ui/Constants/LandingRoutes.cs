using Fonbec.Web.DataAccess.Constants;

namespace Fonbec.Web.Ui.Constants;

public static class LandingRoutes
{
    /// <summary>
    /// Page to open when a signed-in user arrives at the site root.
    /// Null means stay and show the chapter home.
    /// </summary>
    public static string? For(string role, bool canImproveImages) => role switch
    {
        FonbecRole.Admin => NavRoutes.Chapters,
        FonbecRole.Uploader => NavRoutes.FacilitatorStudents,
        FonbecRole.Reviewer => canImproveImages
            ? NavRoutes.ImprovementQueue
            : NavRoutes.ReviewQueue,
        _ => null,
    };
}