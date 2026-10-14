using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.DataModels.Chapters;
using Fonbec.Web.DataAccess.DataModels.Chapters.Input;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Constants;
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
    Task<List<ChaptersListViewModel>> GetChaptersForAdminListAsync();
    Task<List<SelectableModel<int>>> GetAllChaptersForSelectionAsync();
    Task<CrudResult> CreateChapterAsync(CreateChapterInputModel inputModel);
    Task<CrudResult> UpdateChapterAsync(UpdateChapterInputModel inputModel);
}

public class ChapterService(IChapterRepository chapterRepository, IUserRepository userRepository) : IChapterService
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

    public async Task<List<ChaptersListViewModel>> GetChaptersForAdminListAsync()
    {
        var chapters = await GetAllChaptersAsync();
        var directory = await chapterRepository.GetChapterDirectoryAsync();
        var grants = await userRepository.GetUserClaimsOfTypeAsync(FonbecGrants.ClaimType);
        var staffByChapter = directory.Staff
            .GroupBy(member => member.ChapterId)
            .ToDictionary(group => group.Key, group => group.ToList());

        foreach (var chapter in chapters)
        {
            var staff = staffByChapter.GetValueOrDefault(chapter.ChapterId) ?? [];
            chapter.Managers = People(staff, FonbecRole.Manager, grants);
            chapter.Facilitators = People(staff, FonbecRole.Uploader, grants);
            chapter.Reviewers = People(staff, FonbecRole.Reviewer, grants);
            chapter.StudentCount = directory.StudentCounts.GetValueOrDefault(chapter.ChapterId);
            chapter.SponsorCount = directory.SponsorCounts.GetValueOrDefault(chapter.ChapterId);
        }

        return chapters;
    }

    public async Task<List<SelectableModel<int>>> GetAllChaptersForSelectionAsync()
    {
        var chapters = await GetAllChaptersAsync();
        return chapters.Adapt<List<SelectableModel<int>>>();
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

    private static List<ChapterStaffMemberViewModel> People(
        IEnumerable<ChapterStaffMemberDataModel> staff,
        string role,
        IReadOnlyDictionary<int, string> grants) =>
        staff
            .Where(member => member.Role == role)
            .OrderBy(member => member.LastName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(member => member.FirstName, StringComparer.OrdinalIgnoreCase)
            .Select(member => new ChapterStaffMemberViewModel
            {
                UserId = member.UserId,
                FirstName = member.FirstName,
                LastName = member.LastName,
                Email = member.Email,
                PhoneNumber = string.IsNullOrWhiteSpace(member.PhoneNumber) ? null : member.PhoneNumber,
                IsActive = member.IsActive,
                CanBeLockedOut = member.CanBeLockedOut,
                HasLinkedStudents = member.HasLinkedStudents,
                CanImproveImages = CanImproveImages(role, grants.GetValueOrDefault(member.UserId)),
            })
            .ToList();

    private static bool CanImproveImages(string role, string? grants) =>
        role is FonbecRole.Manager or FonbecRole.Reviewer
        && !string.IsNullOrWhiteSpace(grants)
        && grants.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Contains(DocumentPermission.DigitalImprovement, StringComparer.Ordinal);
}
