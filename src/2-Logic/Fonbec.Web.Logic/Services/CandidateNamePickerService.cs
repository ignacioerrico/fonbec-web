using Fonbec.Web.DataAccess.DataModels.Review;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models.Review;

namespace Fonbec.Web.Logic.Services;

public interface ICandidateNamePickerService
{
    /// <summary>Returns <paramref name="count"/> sponsor names: the correct one + (count - 1) random distractors.</summary>
    Task<CandidateNamesViewModel> GetSponsorNameChoicesAsync(int correctSponsorId, int count);

    /// <summary>Returns <paramref name="count"/> student names: the correct one + (count - 1) random distractors.</summary>
    Task<CandidateNamesViewModel> GetStudentNameChoicesAsync(int correctStudentId, int count);
}

public class CandidateNamePickerService(
    IStudentRepository studentRepository,
    ISponsorRepository sponsorRepository) : ICandidateNamePickerService
{
    public async Task<CandidateNamesViewModel> GetSponsorNameChoicesAsync(int correctSponsorId, int count) =>
        await BuildChoicesAsync(
            correctSponsorId,
            count,
            sponsorRepository.GetSponsorNameAsync,
            sponsorRepository.GetRandomSponsorNamesAsync);

    public async Task<CandidateNamesViewModel> GetStudentNameChoicesAsync(int correctStudentId, int count) =>
        await BuildChoicesAsync(
            correctStudentId,
            count,
            studentRepository.GetStudentNameAsync,
            studentRepository.GetRandomStudentNamesAsync);

    private static async Task<CandidateNamesViewModel> BuildChoicesAsync(
        int correctId,
        int count,
        Func<int, Task<CandidateNameDataModel?>> getName,
        Func<int, int, Task<List<CandidateNameDataModel>>> getRandomNames)
    {
        var correct = await getName(correctId);

        var distractorCount = Math.Max(count - 1, 0);
        var distractors = await getRandomNames(correctId, distractorCount);

        var chosen = new List<CandidateNameViewModel>();
        if (correct is not null)
        {
            chosen.Add(ToViewModel(correct));
        }

        chosen.AddRange(distractors.Select(ToViewModel));

        // Shuffle so the correct answer is not always in a fixed position.
        var shuffled = chosen.OrderBy(_ => Random.Shared.Next()).ToList();

        return new CandidateNamesViewModel
        {
            CorrectId = correctId,
            Names = shuffled,
        };
    }

    private static CandidateNameViewModel ToViewModel(CandidateNameDataModel name) =>
        new(name.Id, $"{name.FirstName} {name.LastName}");
}