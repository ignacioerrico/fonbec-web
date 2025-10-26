using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Chapters.Input;
using Fonbec.Web.Logic.Models.Chapters.Input;
using Mapster;

namespace Fonbec.Web.Logic.Tests.Models.Chapters.Input;

public class UpdateChapterInputModelMappingDefinitionsTests : MappingTestBase
{
    [Fact]
    public void Maps_InputModel_To_InputDataModel()
    {
        var input = new UpdateChapterInputModel(
            ChapterId: 123,
            ChapterUpdatedName: "Updated Chapter Name",
            ChapterUpdatedDescription: "Some description"
        );

        var result = input.Adapt<UpdateChapterInputDataModel>(Config);

        result.ChapterId.Should().Be(123);
        result.ChapterUpdatedName.Should().Be("Updated Chapter Name");
        result.ChapterUpdatedDescription.Should().Be("Some description");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("  \t \n \r ")]
    public void Maps_ChapterUpdatedDescription_InputModel_EmptyOrWhitespace_To_InputDataModel_Null(string chapterUpdatedDescription)
    {
        var input = new UpdateChapterInputModel(
            ChapterId: 456,
            ChapterUpdatedName: "Updated Chapter Name",
            ChapterUpdatedDescription: chapterUpdatedDescription
        );

        var result = input.Adapt<UpdateChapterInputDataModel>(Config);

        result.ChapterId.Should().Be(456);
        result.ChapterUpdatedName.Should().Be("Updated Chapter Name");
        result.ChapterUpdatedDescription.Should().BeNull();
    }
}