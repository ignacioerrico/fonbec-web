using Fonbec.Web.DataAccess.DataModels.Chapters.Input;
using Fonbec.Web.Logic.ExtensionMethods;
using Mapster;

namespace Fonbec.Web.Logic.Models.Chapters.Input;

public record UpdateChapterInputModel(
    int ChapterId,
    string ChapterUpdatedName,
    string ChapterUpdatedNotes,
    int UpdatedById
);

public class UpdateChapterInputModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<UpdateChapterInputModel, UpdateChapterInputDataModel>()
            .Map(dest => dest.ChapterId, src => src.ChapterId)
            .Map(dest => dest.ChapterUpdatedName, src => src.ChapterUpdatedName.MustBeNonEmpty().NormalizeText())
            .Map(dest => dest.ChapterUpdatedNotes, src => src.ChapterUpdatedNotes.NullOrTrimmed())
            .Map(dest => dest.UpdatedById, src => src.UpdatedById);
    }
}