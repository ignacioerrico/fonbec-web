using Fonbec.Web.DataAccess.DataModels.Chapters;
using Fonbec.Web.Logic.Models.Students;
using Mapster;

namespace Fonbec.Web.Logic.Models.Chapters;

public class ChaptersListViewModel : AuditableViewModel, IChangableViewModel<ChaptersListViewModel>
{
    public int ChapterId { get; set; }
    public string ChapterName { get; init; } = string.Empty;
    public bool IsChapterActive { get; set; }
    public ChaptersListViewModel DeepClone()
    {
        return this.Adapt<ChaptersListViewModel>();
    }

    public bool Equals(ChaptersListViewModel? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return IsIdenticalTo(other);
    }

    private bool IsIdenticalTo(ChaptersListViewModel other) =>
        ChapterId == other.ChapterId
        && ChapterName == other.ChapterName
        && IsChapterActive == other.IsChapterActive
        && Notes == other.Notes;
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