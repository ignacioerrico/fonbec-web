using FluentAssertions;
using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.DataModels.Chapters;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Constants;
using Fonbec.Web.Logic.Services;
using NSubstitute;

namespace Fonbec.Web.Logic.Tests.Services;

public class ChapterServiceTests
{
    private readonly IChapterRepository _chapterRepository;
    private readonly ChapterService _chapterService;

    public ChapterServiceTests()
    {
        _chapterRepository = Substitute.For<IChapterRepository>();
        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetUserClaimsOfTypeAsync(Arg.Any<string>())
            .Returns(new Dictionary<int, string>());
        _chapterService = new ChapterService(_chapterRepository, userRepository);
    }

    [Fact]
    public async Task GetChapterNameAsync_Returns_ChapterName_When_Found()
    {
        // Arrange
        const int chapterId = 1;
        const string expectedName = "Test Chapter";
        _chapterRepository.GetChapterNameAsync(chapterId).Returns(expectedName);

        // Act
        var result = await _chapterService.GetChapterNameAsync(chapterId);

        // Assert
        result.Should().Be(expectedName);
    }

    [Fact]
    public async Task GetChapterNameAsync_Returns_Null_When_NotFound()
    {
        // Arrange
        const int chapterId = 2;
        _chapterRepository.GetChapterNameAsync(chapterId).Returns((string?)null);

        // Act
        var result = await _chapterService.GetChapterNameAsync(chapterId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetChaptersForAdminListAsync_GroupsStaffByRole_AndAttachesCounts()
    {
        var auditable = new Chapter
        {
            CreatedBy = new FonbecWebUser { FirstName = "Ada", LastName = "Lovelace" },
            Notes = "nota",
        };
        _chapterRepository.GetAllChaptersAsync().Returns(
        [
            new AllChaptersDataModel(auditable)
            {
                ChapterId = 1,
                ChapterName = "Norte",
                IsChapterActive = true,
            },
            new AllChaptersDataModel(auditable)
            {
                ChapterId = 2,
                ChapterName = "Sur",
                IsChapterActive = true,
            },
        ]);
        _chapterRepository.GetChapterDirectoryAsync().Returns(new ChapterDirectoryDataModel
        {
            Staff =
            [
                Member(1, 20, FonbecRole.Uploader, "Nico", "Bazan", "   ", hasLinkedStudents: true),
                Member(1, 11, FonbecRole.Manager, "Ana", "Zapata", "111"),
                Member(1, 10, FonbecRole.Manager, "Luis", "Alvarez", null),
                Member(1, 40, FonbecRole.Reviewer, "Eva", "Ruiz", "555"),
                Member(1, 99, FonbecRole.Admin, "No", "Mostrar", null),
                Member(2, 16, FonbecRole.Manager, "Sur", "Jefe", "777"),
            ],
            StudentCounts = new Dictionary<int, int> { [1] = 4 },
            SponsorCounts = new Dictionary<int, int> { [1] = 2, [2] = 1 },
        });
        var userRepository = Substitute.For<IUserRepository>();
        userRepository.GetUserClaimsOfTypeAsync(FonbecGrants.ClaimType).Returns(new Dictionary<int, string>
        {
            [10] = DocumentPermission.DigitalImprovement,
            [20] = DocumentPermission.DigitalImprovement,
        });
        var chapterService = new ChapterService(_chapterRepository, userRepository);

        var chapters = await chapterService.GetChaptersForAdminListAsync();

        var north = chapters.Single(chapter => chapter.ChapterId == 1);
        north.Managers.Select(person => person.UserId).Should().Equal(10, 11);
        north.Managers[0].CanImproveImages.Should().BeTrue();
        north.Managers[1].CanImproveImages.Should().BeFalse();
        north.Managers[0].PhoneNumber.Should().BeNull();
        north.Managers[1].PhoneNumber.Should().Be("111");
        north.Facilitators.Should().ContainSingle();
        north.Facilitators[0].UserId.Should().Be(20);
        north.Facilitators[0].PhoneNumber.Should().BeNull();
        north.Facilitators[0].HasLinkedStudents.Should().BeTrue();
        north.Facilitators[0].CanImproveImages.Should().BeFalse();
        north.Reviewers.Should().ContainSingle().Which.UserId.Should().Be(40);
        north.Reviewers[0].CanImproveImages.Should().BeFalse();
        north.StudentCount.Should().Be(4);
        north.SponsorCount.Should().Be(2);

        var south = chapters.Single(chapter => chapter.ChapterId == 2);
        south.Managers.Should().ContainSingle().Which.UserId.Should().Be(16);
        south.Facilitators.Should().BeEmpty();
        south.Reviewers.Should().BeEmpty();
        south.StudentCount.Should().Be(0);
        south.SponsorCount.Should().Be(1);
    }

    private static ChapterStaffMemberDataModel Member(
        int chapterId,
        int userId,
        string role,
        string firstName,
        string lastName,
        string? phone,
        bool hasLinkedStudents = false) =>
        new()
        {
            ChapterId = chapterId,
            UserId = userId,
            Role = role,
            FirstName = firstName,
            LastName = lastName,
            Email = $"{firstName}@fonbec.org",
            PhoneNumber = phone,
            IsActive = true,
            CanBeLockedOut = true,
            HasLinkedStudents = hasLinkedStudents,
        };
}
