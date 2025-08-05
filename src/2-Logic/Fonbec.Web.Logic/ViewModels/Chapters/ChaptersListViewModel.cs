using Fonbec.Web.DataAccess.DataModels.Chapters;
using Mapster;

namespace Fonbec.Web.Logic.ViewModels.Chapters;

public class ChaptersListViewModel
{
    public string ChapterName { get; init; } = string.Empty;
}

public class ChaptersListViewModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ChaptersListDataModel, ChaptersListViewModel>()
            .Map(dest => dest.ChapterName, src => src.ChapterName);
    }
}