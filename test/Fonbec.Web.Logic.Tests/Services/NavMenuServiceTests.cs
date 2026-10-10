using FluentAssertions;
using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.DataModels.Documents;
using Fonbec.Web.DataAccess.DataModels.LetterFollowUp;
using Fonbec.Web.DataAccess.DataModels.PlannedDelivery;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models.LetterPlanProgress;
using Fonbec.Web.Logic.Models.Navigation;
using Fonbec.Web.Logic.Services;
using NSubstitute;

namespace Fonbec.Web.Logic.Tests.Services;

public class NavMenuServiceTests
{
    private const int ChapterId = 4;
    private const int PlanId = 7;

    private readonly IStudentRepository _students = Substitute.For<IStudentRepository>();
    private readonly ISponsorRepository _sponsors = Substitute.For<ISponsorRepository>();
    private readonly ICompanyRepository _companies = Substitute.For<ICompanyRepository>();
    private readonly ILetterFollowUpRepository _followUp = Substitute.For<ILetterFollowUpRepository>();
    private readonly IPlannedDeliveryRepository _plans = Substitute.For<IPlannedDeliveryRepository>();
    private readonly ILetterPlanProgressService _progress = Substitute.For<ILetterPlanProgressService>();
    private readonly IDocumentRepository _documents = Substitute.For<IDocumentRepository>();
    private readonly NavMenuService _service;

    public NavMenuServiceTests()
    {
        _students.CountStudentsAsync(Arg.Any<int?>()).Returns(0);
        _sponsors.CountSponsorsAsync(Arg.Any<int?>()).Returns(0);
        _companies.CountCompaniesAsync().Returns(0);
        _followUp.CountOpenFlagsAsync(Arg.Any<int>()).Returns(new OpenFlagCounts(0, 0));
        _plans.GetCurrentPlanAsync(Arg.Any<int>()).Returns((CurrentPlannedDeliveryDataModel?)null);
        _documents.GetGlobalReviewProgressAsync(Arg.Any<int?>()).Returns(new ReviewProgressDataModel());

        _service = new NavMenuService(
            _students,
            _sponsors,
            _companies,
            _followUp,
            _plans,
            _progress,
            _documents);
    }

    [Fact]
    public async Task GetAsync_Manager_Loads_Chapter_Roster_Campaign_Flags_And_Queues()
    {
        _students.CountStudentsAsync(ChapterId).Returns(18);
        _sponsors.CountSponsorsAsync(ChapterId).Returns(9);
        _companies.CountCompaniesAsync().Returns(3);
        _followUp.CountOpenFlagsAsync(ChapterId).Returns(new OpenFlagCounts(2, 5));
        _plans.GetCurrentPlanAsync(ChapterId).Returns(new CurrentPlannedDeliveryDataModel
        {
            PlannedDeliveryId = PlanId,
            PlannedDeliveryStartsOn = new DateTime(2026, 4, 1),
        });
        _progress.GetProgressAsync(PlanId, ChapterId).Returns(Progress(37));
        _documents.GetGlobalReviewProgressAsync(null).Returns(new ReviewProgressDataModel
        {
            PendingLetters = 4,
            PendingReportCards = 1,
            PendingOther = 0,
            PendingImprovement = 6,
        });
        var indicators = await _service.GetAsync(FonbecRole.Manager, ChapterId, canImproveImages: true);

        indicators.StudentCount.Should().Be(18);
        indicators.SponsorCount.Should().Be(9);
        indicators.CompanyCount.Should().Be(3);
        indicators.CampaignCompletionPercent.Should().Be(37);
        indicators.RedFlags.Should().Be(2);
        indicators.GreenFlags.Should().Be(5);
        indicators.PendingReview.Should().Be(5);
        indicators.PendingImprovement.Should().Be(6);
    }

    [Fact]
    public async Task GetAsync_Manager_Hides_Campaign_Percent_When_There_Is_No_Open_Plan()
    {
        _plans.GetCurrentPlanAsync(ChapterId).Returns((CurrentPlannedDeliveryDataModel?)null);

        var indicators = await _service.GetAsync(FonbecRole.Manager, ChapterId, canImproveImages: false);

        indicators.CampaignCompletionPercent.Should().BeNull();
        await _progress.DidNotReceive().GetProgressAsync(Arg.Any<int>(), Arg.Any<int>());
    }

    [Fact]
    public async Task GetAsync_Manager_Shows_Zero_Percent_For_An_Open_Plan_With_Nothing_Approved()
    {
        _plans.GetCurrentPlanAsync(ChapterId).Returns(new CurrentPlannedDeliveryDataModel
        {
            PlannedDeliveryId = PlanId,
        });
        _progress.GetProgressAsync(PlanId, ChapterId).Returns(Progress(0));

        var indicators = await _service.GetAsync(FonbecRole.Manager, ChapterId, canImproveImages: false);

        indicators.CampaignCompletionPercent.Should().Be(0);
    }

    [Fact]
    public async Task GetAsync_Manager_Without_Improvement_Grant_Omits_That_Count()
    {
        _documents.GetGlobalReviewProgressAsync(null).Returns(new ReviewProgressDataModel
        {
            PendingLetters = 2,
            PendingImprovement = 8,
        });

        var indicators = await _service.GetAsync(FonbecRole.Manager, ChapterId, canImproveImages: false);

        indicators.PendingReview.Should().Be(2);
        indicators.PendingImprovement.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_Admin_Counts_The_Roster_And_Skips_Manager_Only_Work()
    {
        _students.CountStudentsAsync(null).Returns(40);
        _sponsors.CountSponsorsAsync(null).Returns(12);

        var indicators = await _service.GetAsync(FonbecRole.Admin, chapterId: null, canImproveImages: false);

        indicators.StudentCount.Should().Be(40);
        indicators.SponsorCount.Should().Be(12);
        indicators.CompanyCount.Should().BeNull();
        indicators.CampaignCompletionPercent.Should().BeNull();
        indicators.RedFlags.Should().Be(0);
        indicators.GreenFlags.Should().Be(0);
        indicators.PendingReview.Should().BeNull();
        indicators.PendingImprovement.Should().BeNull();
        await _companies.DidNotReceive().CountCompaniesAsync();
        await _plans.DidNotReceive().GetCurrentPlanAsync(Arg.Any<int>());
        await _documents.DidNotReceive().GetGlobalReviewProgressAsync(Arg.Any<int?>());
    }

    [Fact]
    public async Task GetAsync_Reviewer_Loads_Only_The_Queues()
    {
        _documents.GetGlobalReviewProgressAsync(null).Returns(new ReviewProgressDataModel
        {
            PendingReportCards = 3,
            PendingImprovement = 1,
        });
        var indicators = await _service.GetAsync(FonbecRole.Reviewer, chapterId: null, canImproveImages: true);

        indicators.StudentCount.Should().BeNull();
        indicators.PendingReview.Should().Be(3);
        indicators.PendingImprovement.Should().Be(1);
        await _students.DidNotReceive().CountStudentsAsync(Arg.Any<int?>());
        await _companies.DidNotReceive().CountCompaniesAsync();
    }

    [Fact]
    public async Task GetAsync_Uploader_Loads_Nothing()
    {
        var indicators = await _service.GetAsync(FonbecRole.Uploader, ChapterId, canImproveImages: false);

        indicators.Should().BeEquivalentTo(NavMenuIndicators.Empty);
        await _students.DidNotReceive().CountStudentsAsync(Arg.Any<int?>());
        await _documents.DidNotReceive().GetGlobalReviewProgressAsync(Arg.Any<int?>());
    }

    private static LetterPlanProgressViewModel Progress(decimal percent) =>
        new()
        {
            PlanLabel = "abril de 2026",
            Summary = new LetterPlanProgressSummaryViewModel
            {
                CompletionPercent = percent,
            },
        };
}