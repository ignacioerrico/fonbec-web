using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Chapters;
using Fonbec.Web.Logic.Models.Chapters;
using Mapster;

namespace Fonbec.Web.Logic.Tests.ViewModels.Chapters;

public class AllChaptersViewModelMappingTests : MappingTestBase
{
    [Fact]
    public void Maps_ChapterName_From_DataModel_To_ViewModel()
    {
        // Arrange
        var dataModel = new AllChaptersDataModel(Auditable)
        {
            ChapterId = 314,
            ChapterName = "Test Chapter"
        };

        // Act
        var viewModel = dataModel.Adapt<AllChaptersViewModel>(Config);

        // Assert
        viewModel.ChapterId.Should().Be(314);
        viewModel.ChapterName.Should().Be("Test Chapter");
    }
}