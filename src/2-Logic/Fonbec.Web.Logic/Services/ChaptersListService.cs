using Fonbec.Web.Logic.ViewModels.Chapters;

namespace Fonbec.Web.Logic.Services;

public interface IChaptersListService
{
    IEnumerable<ChaptersListViewModel> GetAllChapters();
}

public class ChaptersListService : IChaptersListService
{
    public IEnumerable<ChaptersListViewModel> GetAllChapters()
    {
        return new List<ChaptersListViewModel>
        {
            new() { ChapterName = "Buenos Aires" },
            new() { ChapterName = "Mendoza" },
            new() { ChapterName = "Córdoba" }
        };
    }
}