using Fonbec.Web.DataAccess.DataModels;
using Fonbec.Web.DataAccess.DataModels.Chapters;
using Fonbec.Web.Logic.ExtensionMethods;
using Mapster;

namespace Fonbec.Web.Logic.Models.Chapters;

public class ChaptersListViewModel : AuditableViewModel, IDetectChanges<ChaptersListViewModel>
{
    public int ChapterId { get; set; }
    public string ChapterName { get; init; } = string.Empty;
    public bool IsChapterActive { get; set; }

    public bool IsIdenticalTo(ChaptersListViewModel other) =>
        ChapterName == other.ChapterName.NormalizeText()
        && Notes == other.Notes.NullOrTrimmed();
}

public class ChaptersListViewModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<AllChaptersDataModel, ChaptersListViewModel>()
            .Map(dest => dest.ChapterId, src => src.ChapterId)
            .Map(dest => dest.ChapterName, src => src.ChapterName)
            .Map(dest => dest.IsChapterActive, src => src.IsChapterActive);

        config.NewConfig<ChaptersListViewModel, SelectableModel<int>>()
            .Map(dest => dest.Key, src => src.ChapterId)
            .Map(dest => dest.DisplayName, src => src.ChapterName);
    }
}