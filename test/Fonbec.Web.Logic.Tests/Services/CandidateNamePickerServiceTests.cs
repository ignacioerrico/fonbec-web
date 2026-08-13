using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Review;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Services;
using NSubstitute;

namespace Fonbec.Web.Logic.Tests.Services;

public class CandidateNamePickerServiceTests
{
    private readonly IStudentRepository _studentRepository = Substitute.For<IStudentRepository>();
    private readonly ISponsorRepository _sponsorRepository = Substitute.For<ISponsorRepository>();

    private CandidateNamePickerService CreateService() =>
        new(_studentRepository, _sponsorRepository);

    private static CandidateNameDataModel Name(int id) =>
        new() { Id = id, FirstName = $"First{id}", LastName = $"Last{id}" };

    [Fact]
    public async Task SponsorChoices_WithCount5_ReturnsCorrectPlusFourDistinctDistractors()
    {
        const int correctId = 100;
        _sponsorRepository.GetSponsorNameAsync(correctId).Returns(Name(correctId));
        _sponsorRepository.GetRandomSponsorNamesAsync(correctId, 4)
            .Returns([Name(1), Name(2), Name(3), Name(4)]);

        var service = CreateService();

        var result = await service.GetSponsorNameChoicesAsync(correctId, 5);

        result.CorrectId.Should().Be(correctId);
        result.Names.Should().HaveCount(5);
        result.Names.Select(n => n.Id).Should().OnlyHaveUniqueItems();
        result.Names.Select(n => n.Id).Should().Contain(correctId);
        result.Names.Single(n => n.Id == correctId).DisplayName.Should().Be($"First{correctId} Last{correctId}");

        // Distractors requested efficiently server-side: count - 1, excluding the correct id.
        await _sponsorRepository.Received(1).GetRandomSponsorNamesAsync(correctId, 4);
    }

    [Fact]
    public async Task StudentChoices_WithCount5_ReturnsCorrectPlusFourDistinctDistractors()
    {
        const int correctId = 7;
        _studentRepository.GetStudentNameAsync(correctId).Returns(Name(correctId));
        _studentRepository.GetRandomStudentNamesAsync(correctId, 4)
            .Returns([Name(11), Name(12), Name(13), Name(14)]);

        var service = CreateService();

        var result = await service.GetStudentNameChoicesAsync(correctId, 5);

        result.CorrectId.Should().Be(correctId);
        result.Names.Should().HaveCount(5);
        result.Names.Select(n => n.Id).Should().Contain(correctId);
        await _studentRepository.Received(1).GetRandomStudentNamesAsync(correctId, 4);
    }

    [Fact]
    public async Task Choices_WithCount1_ReturnsOnlyCorrectAndRequestsNoDistractors()
    {
        const int correctId = 42;
        _sponsorRepository.GetSponsorNameAsync(correctId).Returns(Name(correctId));
        _sponsorRepository.GetRandomSponsorNamesAsync(correctId, 0).Returns([]);

        var service = CreateService();

        var result = await service.GetSponsorNameChoicesAsync(correctId, 1);

        result.Names.Should().ContainSingle().Which.Id.Should().Be(correctId);
        await _sponsorRepository.Received(1).GetRandomSponsorNamesAsync(correctId, 0);
    }
}