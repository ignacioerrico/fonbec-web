using Fonbec.Web.DataAccess.DataModels.Chapters;
using Mapster;

namespace Fonbec.Web.Logic.Models.Chapters;

public class ChaptersListViewModel : AuditableViewModel
{
    public int ChapterId { get; set; }

    public string ChapterName { get; init; } = string.Empty;

    public string ChapterDescription { get; set; } = string.Empty;

    public bool IsChapterActive { get; set; }
}

public class ChaptersListViewModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<AllChaptersDataModel, ChaptersListViewModel>()
            .Map(dest => dest.ChapterId, src => src.ChapterId)
            .Map(dest => dest.ChapterName, src => src.ChapterName)
            .Map(dest => dest.IsChapterActive, src => src.IsChapterActive)
            .Map(dest => dest.ChapterDescription, src => src.ChapterDescription ?? string.Empty);

        config.NewConfig<ChaptersListViewModel, SelectableModel<int>>()
            .Map(dest => dest.Key, src => src.ChapterId)
            .Map(dest => dest.DisplayName, src => src.ChapterName);

    }
}