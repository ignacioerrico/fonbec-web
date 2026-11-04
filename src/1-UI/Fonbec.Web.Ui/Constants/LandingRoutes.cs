using Fonbec.Web.DataAccess.Constants;

namespace Fonbec.Web.Ui.Constants;

public static class LandingRoutes
{
    /// <summary>
    /// Page to open when a signed-in user arrives at the site root.
    /// A reviewer who can improve images opens that queue only when it still has work;
    /// otherwise they open document review. Null means stay and show the chapter home.
    /// </summary>
    public static string? For(string role, bool canImproveImages, bool hasImagesToImprove = false) => role switch
    {
        FonbecRole.Admin => NavRoutes.Chapters,
        FonbecRole.Uploader => NavRoutes.FacilitatorStudents,
        FonbecRole.Reviewer when canImproveImages && hasImagesToImprove => NavRoutes.ImprovementQueue,
        FonbecRole.Reviewer => NavRoutes.ReviewQueue,
        _ => null,
    };
}