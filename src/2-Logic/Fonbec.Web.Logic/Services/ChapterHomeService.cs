using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models.ChapterHome;
using Fonbec.Web.Logic.Models.LetterPlanProgress;
using Fonbec.Web.Logic.Models.Users;

namespace Fonbec.Web.Logic.Services;

public interface IChapterHomeService
{
    Task<ChapterHomeViewModel> GetAsync(int chapterId, int currentUserId);
}

public sealed class ChapterHomeService(
    IUserService userService,
    IStudentRepository studentRepository,
    ISponsorRepository sponsorRepository,
    IPlannedDeliveryService plannedDeliveryService,
    ILetterPlanProgressService letterPlanProgressService) : IChapterHomeService
{
    public async Task<ChapterHomeViewModel> GetAsync(int chapterId, int currentUserId)
    {
        var usersTask = userService.GetAllUsersAsync(chapterId);
        var studentsTask = studentRepository.CountStudentsAsync(chapterId);
        var sponsorsTask = sponsorRepository.CountSponsorsAsync(chapterId);
        var planTask = plannedDeliveryService.GetCurrentPlanAsync(chapterId);

        await Task.WhenAll(usersTask, studentsTask, sponsorsTask, planTask);

        var users = await usersTask;
        var plan = await planTask;
        var progress = plan is null
            ? null
            : await letterPlanProgressService.GetProgressAsync(plan.PlannedDeliveryId, chapterId);

        return new ChapterHomeViewModel
        {
            Managers = People(users, FonbecRole.Manager, currentUserId),
            Facilitators = People(users, FonbecRole.Uploader, currentUserId),
            Reviewers = People(users, FonbecRole.Reviewer, currentUserId),
            StudentCount = await studentsTask,
            SponsorCount = await sponsorsTask,
            Campaign = plan is null
                ? null
                : new ChapterHomeCampaignViewModel
                {
                    PlanId = plan.PlannedDeliveryId,
                    Label = plan.PlannedDeliveryStartsOnText,
                    Summary = progress?.Summary ?? new LetterPlanProgressSummaryViewModel(),
                },
        };
    }

    private static List<ChapterHomePersonViewModel> People(
        IEnumerable<UsersListViewModel> users,
        string role,
        int currentUserId) =>
        users
            .Where(user => user.IsUserActive && user.UserRole == role)
            .Select(user => new ChapterHomePersonViewModel
            {
                UserId = user.UserId,
                FirstName = user.UserFirstName,
                LastName = user.UserLastName,
                NickName = string.IsNullOrWhiteSpace(user.UserNickName) ? null : user.UserNickName,
                Email = user.UserEmail,
                PhoneNumber = string.IsNullOrWhiteSpace(user.UserPhoneNumber) ? null : user.UserPhoneNumber,
                IsCurrentUser = user.UserId == currentUserId,
            })
            .OrderByDescending(person => person.IsCurrentUser)
            .ThenBy(person => person.LastName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(person => person.FirstName, StringComparer.OrdinalIgnoreCase)
            .ToList();
}