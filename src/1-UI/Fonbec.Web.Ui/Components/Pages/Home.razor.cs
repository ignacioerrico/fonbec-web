using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.Constants;
using Fonbec.Web.Logic.Models.ChapterHome;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Constants;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages;

public partial class Home : AuthenticationRequiredComponentBase
{
    private bool _resolved;
    private bool _missingChapter;
    private ChapterHomeViewModel? _home;

    [Inject]
    public IUserService UserService { get; set; } = null!;

    [Inject]
    public IDocumentService DocumentService { get; set; } = null!;

    [Inject]
    public IChapterHomeService ChapterHomeService { get; set; } = null!;

    private static string Unmatched(int count, string one, string many) =>
        count == 1 ? $"1 {one}" : $"{count} {many}";

    private static Color UnmatchedColor(int count) =>
        count > 0 ? Color.Warning : Color.Secondary;

    private IReadOnlyList<TeamSection> Teams => _home is null
        ? []
        :
        [
            new("Coordinadores", null, "Todavía no hay coordinadores en esta filial.", _home.Managers),
            new("Mediadores", NavRoutes.Mediators, "Todavía no hay mediadores en esta filial.", _home.Facilitators),
            new("Revisores", NavRoutes.Reviewers, "Todavía no hay revisores en esta filial.", _home.Reviewers),
        ];

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        if (Principal is null)
        {
            return;
        }

        var canImproveImages = CanImproveImages();
        var hasImagesToImprove = false;
        if (canImproveImages)
        {
            var progress = await DocumentService.GetGlobalReviewProgressAsync(FonbecClaim.UserId, UserRole, null);
            hasImagesToImprove = progress.PendingImprovement + progress.ProcessingImprovement > 0;
        }

        var destination = LandingRoutes.For(UserRole, canImproveImages, hasImagesToImprove);
        if (destination is not null)
        {
            NavigationManager.NavigateTo(destination, replace: true);
            return;
        }

        if (UserRole != FonbecRole.Manager)
        {
            _resolved = true;
            return;
        }

        if (FonbecClaim.ChapterId is null)
        {
            _missingChapter = true;
            _resolved = true;
            return;
        }

        _home = await ChapterHomeService.GetAsync(FonbecClaim.ChapterId.Value, FonbecClaim.UserId);
        _resolved = true;
    }

    private bool CanImproveImages()
    {
        if (Principal is null || UserRole != FonbecRole.Reviewer)
        {
            return false;
        }

        var grants = UserService.GetFonbecGrantsClaim(Principal);
        return UserService.HasPermission(
            null,
            UserRole,
            DocumentPermission.DigitalImprovement,
            grants);
    }

    private sealed record TeamSection(
        string Title,
        string? Href,
        string EmptyMessage,
        IReadOnlyList<ChapterHomePersonViewModel> People);
}