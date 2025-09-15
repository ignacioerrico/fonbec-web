using Fonbec.Web.DataAccess.DataModels.Chapters.Input;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models.Chapters;
using Fonbec.Web.Logic.Models.Chapters.Input;
using Mapster;

namespace Fonbec.Web.Logic.Services;

public interface IChapterService
{
    Task<List<ChaptersListViewModel>> GetAllChaptersAsync();
    Task<int> CreateChapterAsync(CreateChapterInputModel inputModel);
}

public class ChapterService(IChapterRepository chapterRepository) : IChapterService
{
    public async Task<List<ChaptersListViewModel>> GetAllChaptersAsync()
    {
        var allChaptersDataModel = await chapterRepository.GetAllChaptersAsync();
        var chaptersListViewModel = allChaptersDataModel.Adapt<List<ChaptersListViewModel>>();
        return chaptersListViewModel;
    }

    public async Task<int> CreateChapterAsync(CreateChapterInputModel inputModel)
    {
        var inputDataModel = inputModel.Adapt<CreateChapterInputDataModel>();
        var affectedRows = await chapterRepository.CreateChapterAsync(inputDataModel);
        return affectedRows;
    }
}