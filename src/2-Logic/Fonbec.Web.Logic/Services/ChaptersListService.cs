using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.ViewModels.Chapters;
using Mapster;

namespace Fonbec.Web.Logic.Services;

public interface IChaptersListService
{
    Task<List<ChaptersListViewModel>> GetAllChaptersAsync();
}

public class ChaptersListService(IChaptersListRepository chaptersListRepository) : IChaptersListService
{
    public async Task<List<ChaptersListViewModel>> GetAllChaptersAsync()
    {
        var allChapters = await chaptersListRepository.GetAllChaptersAsync();
        return allChapters.Adapt<List<ChaptersListViewModel>>();
    }
}