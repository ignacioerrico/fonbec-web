using FluentAssertions;
using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models.LetterPlanProgress;
using Fonbec.Web.Logic.Models.PlannedDeliveries;
using Fonbec.Web.Logic.Models.Users;
using Fonbec.Web.Logic.Services;
using NSubstitute;

namespace Fonbec.Web.Logic.Tests.Services;

public class ChapterHomeServiceTests
{
    private const int ChapterId = 4;
    private const int CurrentUserId = 20;

    private readonly IUserService _users = Substitute.For<IUserService>();
    private readonly IStudentRepository _students = Substitute.For<IStudentRepository>();
    private readonly ISponsorRepository _sponsors = Substitute.For<ISponsorRepository>();
    private readonly IPlannedDeliveryService _plans = Substitute.For<IPlannedDeliveryService>();
    private readonly ILetterPlanProgressService _progress = Substitute.For<ILetterPlanProgressService>();
    private readonly ChapterHomeService _service;

    public ChapterHomeServiceTests()
    {
        _users.GetAllUsersAsync(Arg.Any<int?>()).Returns([]);
        _students.CountStudentsAsync(Arg.Any<int?>()).Returns(0);
        _students.CountStudentsWithoutSponsorAsync(Arg.Any<int?>(), Arg.Any<DateTime>()).Returns(0);
        _sponsors.CountSponsorsAsync(Arg.Any<int?>()).Returns(0);
        _sponsors.CountSponsorsWithoutStudentAsync(Arg.Any<int?>(), Arg.Any<DateTime>()).Returns(0);
        _plans.GetCurrentPlanAsync(Arg.Any<int>()).Returns((CurrentPlannedDeliveryViewModel?)null);

        _service = new ChapterHomeService(_users, _students, _sponsors, _plans, _progress, TimeProvider.System);
    }

    [Fact]
    public async Task GetAsync_GroupsActiveUsers_FlagsCurrentUser_AndCountsChapter()
    {
        _users.GetAllUsersAsync(ChapterId).Returns(
        [
            Person(11, FonbecRole.Manager, "Ana", "Zapata", "ana@fonbec.org", "111"),
            Person(CurrentUserId, FonbecRole.Manager, "Luis", "Alvarez", "luis@fonbec.org", "222", nickName: "Lucho"),
            Person(12, FonbecRole.Manager, "Inactivo", "Perez", "off@fonbec.org", "333", active: false),
            Person(30, FonbecRole.Uploader, "Mara", "Diaz", "mara@fonbec.org", ""),
            Person(31, FonbecRole.Uploader, "Nico", "Bazan", "nico@fonbec.org", "444", active: false),
            Person(40, FonbecRole.Reviewer, "Eva", "Ruiz", "eva@fonbec.org", "555"),
            Person(41, FonbecRole.Admin, "Global", "Admin", "admin@fonbec.org", "666"),
        ]);
        _students.CountStudentsAsync(ChapterId).Returns(18);
        _students.CountStudentsWithoutSponsorAsync(ChapterId, Arg.Any<DateTime>()).Returns(4);
        _sponsors.CountSponsorsAsync(ChapterId).Returns(9);
        _sponsors.CountSponsorsWithoutStudentAsync(ChapterId, Arg.Any<DateTime>()).Returns(2);

        var home = await _service.GetAsync(ChapterId, CurrentUserId);

        home.StudentCount.Should().Be(18);
        home.StudentsWithoutSponsor.Should().Be(4);
        home.SponsorCount.Should().Be(9);
        home.SponsorsWithoutStudent.Should().Be(2);
        home.Campaign.Should().BeNull();

        home.Managers.Select(person => person.UserId).Should().Equal(CurrentUserId, 11);
        home.Managers[0].IsCurrentUser.Should().BeTrue();
        home.Managers[0].NickName.Should().Be("Lucho");
        home.Managers[0].Email.Should().Be("luis@fonbec.org");
        home.Managers[0].PhoneNumber.Should().Be("222");
        home.Managers[1].IsCurrentUser.Should().BeFalse();
        home.Managers[1].NickName.Should().BeNull();

        home.Facilitators.Should().ContainSingle();
        home.Facilitators[0].UserId.Should().Be(30);
        home.Facilitators[0].PhoneNumber.Should().BeNull();
        home.Reviewers.Should().ContainSingle()
            .Which.UserId.Should().Be(40);

        await _progress.DidNotReceive().GetProgressAsync(Arg.Any<int>(), Arg.Any<int>());
    }

    [Fact]
    public async Task GetAsync_IncludesTheOpenCampaignStatus()
    {
        var plan = new CurrentPlannedDeliveryViewModel
        {
            PlannedDeliveryId = 9,
            PlannedDeliveryStartsOn = new DateTime(2026, 4, 1),
        };
        _plans.GetCurrentPlanAsync(ChapterId).Returns(plan);
        _progress.GetProgressAsync(9, ChapterId).Returns(new LetterPlanProgressViewModel
        {
            Summary = new LetterPlanProgressSummaryViewModel
            {
                TotalRequired = 10,
                Approved = 3,
                InProgress = 4,
                MissingOrRejected = 3,
                CompletionPercent = 30,
            },
        });

        var home = await _service.GetAsync(ChapterId, CurrentUserId);

        home.Campaign.Should().NotBeNull();
        home.Campaign!.PlanId.Should().Be(9);
        home.Campaign.Label.Should().Be(plan.PlannedDeliveryStartsOnText);
        home.Campaign.Summary.Approved.Should().Be(3);
        home.Campaign.Summary.InProgress.Should().Be(4);
        home.Campaign.Summary.MissingOrRejected.Should().Be(3);
        home.Campaign.Summary.TotalRequired.Should().Be(10);
        await _progress.Received(1).GetProgressAsync(9, ChapterId);
    }

    private static UsersListViewModel Person(
        int id,
        string role,
        string firstName,
        string lastName,
        string email,
        string phone,
        string nickName = "",
        bool active = true) =>
        new()
        {
            UserId = id,
            UserRole = role,
            UserFirstName = firstName,
            UserLastName = lastName,
            UserNickName = nickName,
            UserEmail = email,
            UserPhoneNumber = phone,
            IsUserActive = active,
        };
}