using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.ViewModels.Chapters;
using Mapster;

namespace Fonbec.Web.Logic.Services;

public interface IChaptersListService
{
    Task<List<AllChaptersViewModel>> GetAllChaptersAsync();
}

public class ChaptersListService(IChapterRepository chapterRepository) : IChaptersListService
{
    public async Task<List<AllChaptersViewModel>> GetAllChaptersAsync()
    {
        var allChapters = await chapterRepository.GetAllChaptersAsync();
        return allChapters.Adapt<List<AllChaptersViewModel>>();
    }
}