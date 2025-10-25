using Fonbec.Web.DataAccess.DataModels.Chapters.Input;
using Fonbec.Web.Logic.ExtensionMethods;
using Mapster;

namespace Fonbec.Web.Logic.Models.Chapters.Input;

public record UpdateChapterInputModel(
    int ChapterId,
    string ChapterUpdatedName, 
    string? ChapterUpdatedDescription
);

public class UpdateChapterInputModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<UpdateChapterInputModel, UpdateChapterInputDataModel>()
            .Map(dest => dest.ChapterId, src => src.ChapterId)
            .Map(dest => dest.ChapterUpdatedName, src => src.ChapterUpdatedName.NormalizeText())
            .Map(dest => dest.ChapterUpdatedDescription, src => string.IsNullOrWhiteSpace(src.ChapterUpdatedDescription) ? null : src.ChapterUpdatedDescription.NormalizeText());

    }
}