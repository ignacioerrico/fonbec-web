using Fonbec.Web.DataAccess.DataModels.Chapters.Input;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models;
using Fonbec.Web.Logic.Models.Chapters;
using Fonbec.Web.Logic.Models.Chapters.Input;
using Fonbec.Web.Logic.Models.Results;
using Mapster;

namespace Fonbec.Web.Logic.Services;

public interface IChapterService
{
    Task<string?> GetChapterNameAsync(int chapterId);
    Task<List<ChaptersListViewModel>> GetAllChaptersAsync();
    Task<List<SelectableModel<int>>> GetAllChaptersForSelectionAsync();
    Task<CrudResult> CreateChapterAsync(CreateChapterInputModel inputModel);
    Task<CrudResult> UpdateChapterAsync(UpdateChapterInputModel inputModel);
}

public class ChapterService(IChapterRepository chapterRepository) : IChapterService
{
    public async Task<string?> GetChapterNameAsync(int chapterId)
    {
        return await chapterRepository.GetChapterNameAsync(chapterId);
    }

    public async Task<List<ChaptersListViewModel>> GetAllChaptersAsync()
    {
        var allChaptersDataModel = await chapterRepository.GetAllChaptersAsync();
        var chaptersListViewModel = allChaptersDataModel.Adapt<List<ChaptersListViewModel>>();
        return chaptersListViewModel;
    }

    public async Task<List<SelectableModel<int>>> GetAllChaptersForSelectionAsync()
    {
        return await GetAllChaptersAsync()
            .ContinueWith(t => t.Result.Adapt<List<SelectableModel<int>>>());
    }

    public async Task<CrudResult> CreateChapterAsync(CreateChapterInputModel inputModel)
    {
        var inputDataModel = inputModel.Adapt<CreateChapterInputDataModel>();
        var affectedRows = await chapterRepository.CreateChapterAsync(inputDataModel);
        return new CrudResult(affectedRows);
    }

    public async Task<CrudResult> UpdateChapterAsync(UpdateChapterInputModel inputModel)
    {
        var updateChapterInputDataModel = inputModel.Adapt<UpdateChapterInputDataModel>();
        var affectedRows = await chapterRepository.UpdateChapterAsync(updateChapterInputDataModel);
        return new CrudResult(affectedRows);
    }
}