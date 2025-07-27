using Fonbec.Web.Ui.Models;

namespace Fonbec.Web.Logic.Services;

public interface IListChaptersService
{
    IEnumerable<Chapter> GetAllChapters();
}

public class ListChaptersService : IListChaptersService
{
    public IEnumerable<Chapter> GetAllChapters()
    {
        return new List<Chapter>
        {
            new Chapter { Name = "Buenos Aires" },
            new Chapter { Name = "Mendoza" },
            new Chapter { Name = "Córdoba" }
        };
    }
}