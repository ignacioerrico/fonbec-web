using Fonbec.Web.DataAccess.DataModels.Chapters.Input;
using Mapster;

namespace Fonbec.Web.Logic.Models.Chapters.Input;

public record CreateChapterInputModel(
    string ChapterName,
    int ChapterCreatedById
);

public class CreateChapterInputModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateChapterInputModel, CreateChapterInputDataModel>()
            .Map(dest => dest.ChapterName, src => src.ChapterName)
            .Map(dest => dest.ChapterCreatedById, src => src.ChapterCreatedById);
    }
}