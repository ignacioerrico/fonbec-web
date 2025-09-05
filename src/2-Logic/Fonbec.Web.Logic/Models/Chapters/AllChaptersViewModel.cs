using Fonbec.Web.DataAccess.DataModels.Chapters;
using Fonbec.Web.Logic.Models;
using Mapster;

namespace Fonbec.Web.Logic.Models.Chapters;

public class AllChaptersViewModel : AuditableViewModel
{
    public int ChapterId { get; set; }

    public string ChapterName { get; init; } = string.Empty;
}

public class AllChaptersViewModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<AllChaptersDataModel, AllChaptersViewModel>()
            .Map(dest => dest.ChapterId, src => src.ChapterId)
            .Map(dest => dest.ChapterName, src => src.ChapterName);
    }
}