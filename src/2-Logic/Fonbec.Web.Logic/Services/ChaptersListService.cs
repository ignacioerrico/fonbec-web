using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.ViewModels.Chapters;

namespace Fonbec.Web.Logic.Services;

public interface IChaptersListService
{
    Task<IEnumerable<ChaptersListViewModel>> GetAllChaptersAsync();
}

public class ChaptersListService : IChaptersListService
{
    private readonly IChaptersListRepository _chaptersListRepository;

    public ChaptersListService(IChaptersListRepository chaptersListRepository)
    {
        _chaptersListRepository = chaptersListRepository;
    }

    public async Task<IEnumerable<ChaptersListViewModel>> GetAllChaptersAsync()
    {
        var allChapters = await _chaptersListRepository.GetAllChaptersAsync();
        return allChapters.Select(chapter => new ChaptersListViewModel
        {
            ChapterName = chapter.Name
        });
    }
}