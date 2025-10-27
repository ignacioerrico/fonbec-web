using Fonbec.Web.DataAccess.DataModels.Chapters.Input;
using Fonbec.Web.Logic.ExtensionMethods;
using Mapster;

namespace Fonbec.Web.Logic.Models.Chapters.Input;

public record CreateChapterInputModel(
    string ChapterName,
    string ChapterNotes,
    int ChapterCreatedById
);

public class CreateChapterInputModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateChapterInputModel, CreateChapterInputDataModel>()
            .Map(dest => dest.ChapterName, src => src.ChapterName.MustBeNonEmpty().NormalizeText())
            .Map(dest => dest.ChapterNotes, src => src.ChapterNotes.NullOrTrimmed())
            .Map(dest => dest.ChapterCreatedById, src => src.ChapterCreatedById);
    }
}