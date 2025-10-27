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
            ChapterName: "Chapter X",
            ChapterCreatedById: 42,
            ChapterDescription: "A description"
        );

        var result = input.Adapt<CreateChapterInputDataModel>(Config);

        result.ChapterName.Should().Be("Chapter X");
        result.ChapterCreatedById.Should().Be(42);
        result.ChapterDescription.Should().Be("A description");
    }

    [Fact]
    public void InputModel_ChapterName_MustBeNonEmpty()
    {
        var input = new CreateChapterInputModel(
            ChapterName: string.Empty,
            ChapterCreatedById: 42,
            ChapterDescription: "A description"
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
            ChapterCreatedById: 42,
            ChapterDescription: "A description"
        );

        var result = input.Adapt<CreateChapterInputDataModel>(Config);

        result.ChapterName.Should().Be("Chapter Name");
    }

    [Fact]
    public void Maps_InputModel_ChapterDescription_To_Trimmed_InputDataModel()
    {
        var input = new CreateChapterInputModel(
            ChapterName: "Chapter X",
            ChapterCreatedById: 42,
            ChapterDescription: "  A description with trailing spaces   "
        );

        var result = input.Adapt<CreateChapterInputDataModel>(Config);

        result.ChapterDescription.Should().Be("A description with trailing spaces");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("  \t \n \r ")]
    public void Maps_EmptyOrWhitespace_InputModel_ChapterDescription_To_InputDataModel_Null(string chapterDescription)
    {
        var input = new CreateChapterInputModel(
            ChapterName: "Chapter X",
            ChapterCreatedById: 42,
            ChapterDescription: chapterDescription
        );

        var result = input.Adapt<CreateChapterInputDataModel>(Config);

        result.ChapterDescription.Should().BeNull();
    }
}