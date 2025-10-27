using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Chapters.Input;
using Fonbec.Web.Logic.Models.Chapters.Input;
using Mapster;

namespace Fonbec.Web.Logic.Tests.Models.Chapters.Input;

public class CreateChapterInputModelMappingDefinitionsTests : MappingTestBase
{
    [Fact]
    public void Maps_InputModel_To_InputDataModel()
    {
        var input = new CreateChapterInputModel(
            ChapterName: "Test Chapter",
            ChapterNotes: "Some personal notes",
            ChapterCreatedById: 42
        );

        var result = input.Adapt<CreateChapterInputDataModel>(Config);

        result.ChapterName.Should().Be("Test Chapter");
        result.ChapterNotes.Should().Be("Some personal notes");
        result.ChapterCreatedById.Should().Be(42);
    }

    [Fact]
    public void InputModel_ChapterName_MustBeNonEmpty()
    {
        var input = new CreateChapterInputModel(
            ChapterName: string.Empty,
            ChapterNotes: "Some personal notes",
            ChapterCreatedById: 42
        );

        var result = () => input.Adapt<CreateChapterInputDataModel>(Config);

        result.Should().Throw<ArgumentException>()
            .WithMessage("String must be non-empty. (Parameter 'value')");
    }

    [Fact]
    public void InputModel_ChapterName_IsNormalized()
    {
        var input = new CreateChapterInputModel(
            ChapterName: "  chApter naMe   ",
            ChapterNotes: "Some personal notes",
            ChapterCreatedById: 42
        );

        var result = input.Adapt<CreateChapterInputDataModel>(Config);

        result.ChapterName.Should().Be("Chapter Name");
    }

    [Fact]
    public void Maps_InputModel_ChapterNotes_To_Trimmed_InputDataModel()
    {
        var input = new CreateChapterInputModel(
            ChapterName: "Test Chapter",
            ChapterNotes: "  Some personal notes   ",
            ChapterCreatedById: 42
        );

        var result = input.Adapt<CreateChapterInputDataModel>(Config);

        result.ChapterNotes.Should().Be("Some personal notes");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("  \t \n \r ")]
    public void Maps_EmptyOrWhitespace_InputModel_ChapterNotes_To_InputDataModel_Null(string chapterNotes)
    {
        var input = new CreateChapterInputModel(
            ChapterName: "Test Chapter",
            ChapterNotes: chapterNotes,
            ChapterCreatedById: 42
        );

        var result = input.Adapt<CreateChapterInputDataModel>(Config);

        result.ChapterNotes.Should().BeNull();
    }
}