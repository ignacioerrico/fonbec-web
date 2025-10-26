using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Chapters;
using Fonbec.Web.Logic.Models.Chapters;
using Mapster;

namespace Fonbec.Web.Logic.Tests.Models.Chapters;

public class ChaptersListViewModelMappingDefinitionsTests : MappingTestBase
{
    [Fact]
    public void Maps_Chapter_From_DataModel_To_ViewModel()
    {
        // Arrange
        var dataModel = new AllChaptersDataModel(Auditable)
        {
            ChapterId = 314,
            ChapterName = "Test Chapter",
            ChapterDescription = "A description",
        };

        // Act
        var viewModel = dataModel.Adapt<ChaptersListViewModel>(Config);

        // Assert
        viewModel.ChapterId.Should().Be(314);
        viewModel.ChapterName.Should().Be("Test Chapter");
        viewModel.ChapterDescription.Should().Be("A description");
    }

    [Fact]
    public void Maps_DataModel_Null_Description_To_ViewModel_EmptyString()
    {
        // Arrange
        var dataModel = new AllChaptersDataModel(Auditable)
        {
            ChapterDescription = null,
        };

        // Act
        var viewModel = dataModel.Adapt<ChaptersListViewModel>(Config);

        // Assert
        viewModel.ChapterDescription.Should().BeEmpty();
    }
}