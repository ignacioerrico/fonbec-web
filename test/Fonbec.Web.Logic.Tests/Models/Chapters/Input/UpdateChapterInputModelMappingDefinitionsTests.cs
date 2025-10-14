using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Chapters.Input;
using Fonbec.Web.Logic.Models.Chapters.Input;
using Mapster;

namespace Fonbec.Web.Logic.Tests.Models.Chapters.Input;

public class UpdateChapterInputModelMappingDefinitionsTests : MappingTestBase
{
    [Fact]
    public void Maps_All_Fields_Correctly()
    {
        var input = new UpdateChapterInputModel(
            ChapterId: 123,
            ChapterUpdatedName: "Updated Chapter Name"
        );

        var result = input.Adapt<UpdateChapterInputDataModel>(Config);

        result.ChapterId.Should().Be(123);
        result.ChapterUpdatedName.Should().Be("Updated Chapter Name");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Maps_ChapterUpdatedName_When_Null_Or_Whitespace(string? updatedName)
    {
        var input = new UpdateChapterInputModel(
            ChapterId: 456,
            ChapterUpdatedName: updatedName!
        );

        var result = input.Adapt<UpdateChapterInputDataModel>(Config);

        result.ChapterId.Should().Be(456);
        result.ChapterUpdatedName.Should().Be(updatedName);
    }
}