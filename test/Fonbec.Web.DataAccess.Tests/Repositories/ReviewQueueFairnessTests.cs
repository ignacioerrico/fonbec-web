using FluentAssertions;
using Fonbec.Web.DataAccess.Repositories;

namespace Fonbec.Web.DataAccess.Tests.Repositories;

public class ReviewQueueFairnessTests
{
    [Fact]
    public void NextChapterAfter_NullCursor_ReturnsSmallestEligible()
    {
        DocumentRepository.NextChapterAfter([1, 2, 3], lastServedChapterId: null).Should().Be(1);
    }

    [Fact]
    public void NextChapterAfter_WrapsFromLastChapterToFirst()
    {
        DocumentRepository.NextChapterAfter([1, 2, 3], lastServedChapterId: 3).Should().Be(1);
    }

    [Fact]
    public void NextChapterAfter_SkipsMissingChaptersAndPicksNextGreaterId()
    {
        DocumentRepository.NextChapterAfter([1, 3], lastServedChapterId: 1).Should().Be(3);
        DocumentRepository.NextChapterAfter([1, 3], lastServedChapterId: 2).Should().Be(3);
    }

    [Fact]
    public void NextChapterAfter_EmptyList_Throws()
    {
        var act = () => DocumentRepository.NextChapterAfter([], lastServedChapterId: 1);
        act.Should().Throw<ArgumentException>();
    }
}