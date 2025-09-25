using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Chapters.Input;
using Fonbec.Web.Logic.Models.Chapters.Input;
using Mapster;

namespace Fonbec.Web.Logic.Tests.Models.Chapters.Input;

public class CreateChapterInputModelMappingDefinitionsTests : MappingTestBase
{
    [Fact]
    public void Maps_All_Fields_Correctly()
    {
        var input = new CreateChapterInputModel(
            ChapterName: "Chapter X",
            ChapterCreatedById: 42
        );

        var result = input.Adapt<CreateChapterInputDataModel>(Config);

        result.ChapterName.Should().Be("Chapter X");
        result.ChapterCreatedById.Should().Be(42);
    }
}