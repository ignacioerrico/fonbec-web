using Fonbec.Web.DataAccess.DataModels.Chapters.Input;
using Fonbec.Web.Logic.ExtensionMethods;
using Mapster;

namespace Fonbec.Web.Logic.Models.Chapters.Input;

public record CreateChapterInputModel(
    string ChapterName,
    int ChapterCreatedById,
    string? ChapterDescription
);

public class CreateChapterInputModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateChapterInputModel, CreateChapterInputDataModel>()
            .Map(dest => dest.ChapterName, src => src.ChapterName.NormalizeText())
            .Map(dest => dest.ChapterCreatedById, src => src.ChapterCreatedById)
            .Map(dest => dest.ChapterDescription, src => string.IsNullOrWhiteSpace(src.ChapterDescription) ? null : src.ChapterDescription.NormalizeText());
    }
}