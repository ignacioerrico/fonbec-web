using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Chapters;
using Fonbec.Web.Logic.ViewModels.Chapters;
using Mapster;

namespace Fonbec.Web.Logic.Tests.ViewModels.Chapters;

public class ChaptersListViewModelMappingTests : MappingTestBase
{
    [Fact]
    public void Maps_ChapterName_From_DataModel_To_ViewModel()
    {
        // Arrange
        var dataModel = new ChaptersListDataModel
        {
            ChapterName = "Test Chapter"
        };

        // Act
        var viewModel = dataModel.Adapt<ChaptersListViewModel>(Config);

        // Assert
        viewModel.ChapterName.Should().Be("Test Chapter");
    }
}