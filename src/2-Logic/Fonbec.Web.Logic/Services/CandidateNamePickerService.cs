using Fonbec.Web.DataAccess.DataModels.Review;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models.Review;

namespace Fonbec.Web.Logic.Services;

public interface ICandidateNamePickerService
{
    /// <summary>
    /// Returns up to <paramref name="count"/> addressee names: the correct recipient plus
    /// distractors drawn from both person-sponsors and companies. The letter recipient is XOR
    /// (<paramref name="sponsorId"/> or <paramref name="companyId"/>). The names and their order
    /// are deterministic for <paramref name="documentId"/> so reloading the review page does not
    /// change the challenge.
    /// </summary>
    Task<CandidateNamesViewModel> GetAddresseeNameChoicesAsync(
        long documentId,
        int? sponsorId,
        int? companyId,
        int count);

    /// <summary>
    /// Returns <paramref name="count"/> student names: the correct one plus distractors.
    /// Deterministic for <paramref name="documentId"/>.
    /// </summary>
    Task<CandidateNamesViewModel> GetStudentNameChoicesAsync(long documentId, int correctStudentId, int count);
}

public class CandidateNamePickerService(
    IStudentRepository studentRepository,
    ISponsorRepository sponsorRepository,
    ICompanyRepository companyRepository) : ICandidateNamePickerService
{
    private const int AddresseeSalt = 1;
    private const int SignerSalt = 2;

    public async Task<CandidateNamesViewModel> GetAddresseeNameChoicesAsync(
        long documentId,
        int? sponsorId,
        int? companyId,
        int count)
    {
        if (sponsorId.HasValue == companyId.HasValue)
        {
            throw new ArgumentException("The addressee must be exactly one of a person-sponsor or a company.");
        }

        var correctIsCompany = companyId.HasValue;
        var correctId = correctIsCompany ? companyId!.Value : sponsorId!.Value;

        var correct = correctIsCompany
            ? await companyRepository.GetCompanyNameAsync(correctId)
            : await sponsorRepository.GetSponsorNameAsync(correctId);

        var chosen = new List<CandidateNameViewModel>();
        if (correct is not null)
        {
            chosen.Add(ToViewModel(correct));
        }

        var distractorCount = Math.Max(count - 1, 0);
        if (distractorCount > 0)
        {
            var excludeSponsorId = correctIsCompany ? null : sponsorId;
            var excludeCompanyId = correctIsCompany ? companyId : null;

            var sponsorPool = await sponsorRepository.GetSponsorCandidateNamesAsync(excludeSponsorId);
            var companyPool = await companyRepository.GetCompanyCandidateNamesAsync(excludeCompanyId);

            var rng = RandomFor(documentId, AddresseeSalt);
            var shuffledSponsors = Shuffle(sponsorPool.Select(ToViewModel).ToList(), rng);
            var shuffledCompanies = Shuffle(companyPool.Select(ToViewModel).ToList(), rng);

            // Alternate kinds so a sparse company (or person) pool still appears in the list
            // instead of being dropped when a mixed union is truncated.
            var sponsorsFirst = rng.Next(2) == 0;
            var interleaved = sponsorsFirst
                ? Interleave(shuffledSponsors, shuffledCompanies)
                : Interleave(shuffledCompanies, shuffledSponsors);

            chosen.AddRange(interleaved.Take(distractorCount));
        }

        var displayRng = RandomFor(documentId, AddresseeSalt);
        return new CandidateNamesViewModel
        {
            CorrectIsCompany = correctIsCompany,
            CorrectId = correctId,
            Names = Shuffle(chosen, displayRng),
        };
    }

    public async Task<CandidateNamesViewModel> GetStudentNameChoicesAsync(
        long documentId,
        int correctStudentId,
        int count)
    {
        var correct = await studentRepository.GetStudentNameAsync(correctStudentId);

        var chosen = new List<CandidateNameViewModel>();
        if (correct is not null)
        {
            chosen.Add(ToViewModel(correct));
        }

        var distractorCount = Math.Max(count - 1, 0);
        if (distractorCount > 0)
        {
            var pool = await studentRepository.GetStudentCandidateNamesAsync(correctStudentId);
            var rng = RandomFor(documentId, SignerSalt);
            chosen.AddRange(Shuffle(pool.Select(ToViewModel).ToList(), rng).Take(distractorCount));
        }

        var displayRng = RandomFor(documentId, SignerSalt);
        return new CandidateNamesViewModel
        {
            CorrectIsCompany = false,
            CorrectId = correctStudentId,
            Names = Shuffle(chosen, displayRng),
        };
    }

    /// <summary>
    /// Builds a <see cref="Random"/> whose seed is a function of the document and which challenge
    /// this is (addressee vs signer). <see cref="Random"/> is stable for a given seed within a .NET
    /// runtime version; a document reviewed after a runtime upgrade may get a different list, and
    /// that list is itself stable. Do not use <see cref="HashCode.Combine"/>: its seed is randomized
    /// per process, so the list would change on every app restart.
    /// </summary>
    private static Random RandomFor(long documentId, int challengeSalt)
    {
        // Knuth's 64-bit LCG constants (MMIX). Consecutive document ids and the two challenge salts
        // would otherwise produce similar 32-bit seeds. The multiplier spreads document ids; the
        // increment separates addressee from signer; XOR-folding the halves feeds both into Random.
        // Further reading: https://en.wikipedia.org/wiki/Linear_congruential_generator
        var mixed = (documentId * 6364136223846793005L) + (challengeSalt * 1442695040888963407L);
        return new Random((int)(mixed ^ (mixed >>> 32)));
    }

    /// <summary>
    /// Fisher–Yates shuffle in place, driven by <paramref name="rng"/> so the same seed always
    /// yields the same order.
    /// Further reading: https://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle
    /// </summary>
    private static List<T> Shuffle<T>(List<T> items, Random rng)
    {
        for (var i = items.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }

        return items;
    }

    /// <summary>
    /// Alternates items from <paramref name="first"/> and <paramref name="second"/> so a sparse
    /// pool (e.g. few companies) still appears among the distractors instead of being dropped when
    /// a mixed list is truncated.
    /// </summary>
    private static List<T> Interleave<T>(IReadOnlyList<T> first, IReadOnlyList<T> second)
    {
        var result = new List<T>(first.Count + second.Count);
        var i = 0;
        var j = 0;
        while (i < first.Count || j < second.Count)
        {
            if (i < first.Count)
            {
                result.Add(first[i++]);
            }

            if (j < second.Count)
            {
                result.Add(second[j++]);
            }
        }

        return result;
    }

    private static CandidateNameViewModel ToViewModel(CandidateNameDataModel name) =>
        new(name.IsCompany, name.Id, $"{name.FirstName} {name.LastName}".Trim());
}