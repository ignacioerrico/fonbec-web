using FluentAssertions;
using Fonbec.Web.DataAccess.Repositories;
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
        _chapterService = new ChapterService(_chapterRepository);
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
}