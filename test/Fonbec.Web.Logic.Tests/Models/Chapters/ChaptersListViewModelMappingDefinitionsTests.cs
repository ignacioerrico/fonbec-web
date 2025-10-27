using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Chapters;
using Fonbec.Web.Logic.Models.Chapters;
using Mapster;

namespace Fonbec.Web.Logic.Tests.Models.Chapters;

public class ChaptersListViewModelMappingDefinitionsTests : MappingTestBase
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Maps_Chapter_From_DataModel_To_ViewModel(bool isChapterActive)
    {
        // Arrange
        var dataModel = new AllChaptersDataModel(Auditable)
        {
            ChapterId = 314,
            ChapterName = "Test Chapter",
            IsChapterActive = isChapterActive
        };

        // Act
        var viewModel = dataModel.Adapt<ChaptersListViewModel>(Config);

        // Assert
        viewModel.ChapterId.Should().Be(314);
        viewModel.ChapterName.Should().Be("Test Chapter");
        viewModel.IsChapterActive.Should().Be(isChapterActive);
    }
}