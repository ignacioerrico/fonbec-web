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
            ChapterUpdatedNotes: "Some personal notes",
            UpdatedById: 4
        );

        var result = input.Adapt<UpdateChapterInputDataModel>(Config);

        result.ChapterId.Should().Be(123);
        result.ChapterUpdatedName.Should().Be("Updated Chapter Name");
        result.ChapterUpdatedNotes.Should().Be("Some personal notes");
        result.UpdatedById.Should().Be(4);
    }

    [Fact]
    public void InputModel_ChapterName_MustBeNonEmpty()
    {
        var input = new UpdateChapterInputModel(
            ChapterId: 123,
            ChapterUpdatedName: string.Empty,
            ChapterUpdatedNotes: "Some personal notes",
            UpdatedById: 4
        );

        var result = () => input.Adapt<UpdateChapterInputDataModel>(Config);

        result.Should().Throw<ArgumentException>()
            .WithMessage("String must be non-empty. (Parameter 'value')");
    }

    [Fact]
    public void InputModel_ChapterName_IsNormalized()
    {
        var input = new UpdateChapterInputModel(
            ChapterId: 123,
            ChapterUpdatedName: "  uPdated cHapter nAME  ",
            ChapterUpdatedNotes: "Some personal notes",
            UpdatedById: 4
        );

        var result = input.Adapt<UpdateChapterInputDataModel>(Config);

        result.ChapterUpdatedName.Should().Be("Updated Chapter Name");
    }

    [Fact]
    public void Maps_InputModel_ChapterUpdatedNotes_To_Trimmed_InputDataModel()
    {
        var input = new UpdateChapterInputModel(
            ChapterId: 123,
            ChapterUpdatedName: "Updated Chapter Name",
            ChapterUpdatedNotes: "  Some personal notes with trailing spaces   ",
            UpdatedById: 4
        );

        var result = input.Adapt<UpdateChapterInputDataModel>(Config);

        result.ChapterUpdatedNotes.Should().Be("Some personal notes with trailing spaces");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("  \t \n \r ")]
    public void Maps_EmptyOrWhitespace_InputModel_ChapterUpdatedNotes_To_InputDataModel_Null(string chapterUpdatedNotes)
    {
        var input = new UpdateChapterInputModel(
            ChapterId: 456,
            ChapterUpdatedName: "Updated Chapter Name",
            ChapterUpdatedNotes: chapterUpdatedNotes,
            UpdatedById: 4
        );

        var result = input.Adapt<UpdateChapterInputDataModel>(Config);

        result.ChapterUpdatedNotes.Should().BeNull();
    }
}