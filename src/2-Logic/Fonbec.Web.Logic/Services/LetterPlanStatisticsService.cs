using System.Globalization;
using Fonbec.Web.DataAccess.DataModels.LetterPlanStatistics;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models.LetterPlanProgress;
using Fonbec.Web.Logic.Models.LetterPlanStatistics;

namespace Fonbec.Web.Logic.Services;

public interface ILetterPlanStatisticsService
{
    Task<LetterPlanStatisticsViewModel?> GetStatisticsAsync(int planId, int managerChapterId);
}

public class LetterPlanStatisticsService(ILetterPlanStatisticsRepository repository) : ILetterPlanStatisticsService
{
    private static readonly CultureInfo EsAr = CultureInfo.GetCultureInfo("es-AR");

    private static readonly string[] DayLabels =
    [
        "Menos de 1 día",
        "1 a 3 días",
        "4 a 7 días",
        "8 a 14 días",
        "15 a 30 días",
        "Más de 30 días",
    ];

    private static readonly string[] MinuteLabels =
    [
        "Menos de 5 min",
        "5 a 15 min",
        "15 a 30 min",
        "30 a 60 min",
        "Más de 1 hora",
    ];

    public async Task<LetterPlanStatisticsViewModel?> GetStatisticsAsync(int planId, int managerChapterId)
    {
        var result = await repository.GetStatisticsAsync(planId, managerChapterId);
        return result is null ? null : Map(result);
    }

    internal static LetterPlanStatisticsViewModel Map(LetterPlanStatisticsQueryResultDataModel result)
    {
        var letters = result.Letters;
        var approved = letters.Where(l => l.Status == DocumentStatus.Approved).ToList();
        var rejected = letters.Where(l => l.Status == DocumentStatus.Rejected).ToList();

        return new LetterPlanStatisticsViewModel
        {
            PlanLabel = LetterPlanProgressFormatting.FormatPlanLabel(result.PlanStartsOn),
            WrittenToNotification = MapWrittenToNotification(approved),
            WaitForImprovement = MapWaitForImprovement(letters),
            WaitForReview = MapWaitForReview(letters),
            ImprovementDuration = MapImprovementDuration(letters),
            ReviewDuration = MapReviewDuration(letters),
            RejectionsByReviewer = MapRejectionsByReviewer(rejected),
            ReviewerActivity = MapReviewerActivity(approved, rejected),
            RejectionReasons = MapRejectionReasons(rejected),
            Outcomes = MapOutcomes(approved.Count, rejected.Count, result.ExemptionCount),
            Scores = MapScores(approved),
        };
    }

    private static DurationChartViewModel MapWrittenToNotification(
        IReadOnlyList<LetterPlanStatisticsLetterDataModel> approved)
    {
        var days = new List<int>();
        var unnotified = 0;
        var missing = 0;

        foreach (var letter in approved)
        {
            if (letter.ConfirmedWrittenDate is null)
            {
                missing++;
                continue;
            }

            if (letter.AddresseeNotificationSentOn is null)
            {
                unnotified++;
                continue;
            }

            var elapsed = (letter.AddresseeNotificationSentOn.Value.Date - letter.ConfirmedWrittenDate.Value.Date).Days;
            days.Add(Math.Max(0, elapsed));
        }

        return Histogram(
            DayLabels,
            days.Select(day => DayBucket(day)),
            days.Count == 0 ? null : CalendarSummary(days),
            JoinNotes(
                CountNote(unnotified, "carta sin aviso al padrino.", "cartas sin aviso al padrino."),
                CountNote(missing, "carta sin este dato.", "cartas sin este dato.")));
    }

    private static DurationChartViewModel MapWaitForImprovement(
        IReadOnlyList<LetterPlanStatisticsLetterDataModel> letters)
    {
        var durations = new List<TimeSpan>();
        var missing = 0;

        foreach (var letter in letters.Where(IsImage))
        {
            if (letter.ImprovementFirstLockedOn is null)
            {
                missing++;
                continue;
            }

            durations.Add(NonNegative(letter.ImprovementFirstLockedOn.Value - letter.UploadedOn));
        }

        return ElapsedDayHistogram(durations, missing);
    }

    private static DurationChartViewModel MapWaitForReview(
        IReadOnlyList<LetterPlanStatisticsLetterDataModel> letters)
    {
        var durations = new List<TimeSpan>();
        var missing = 0;

        foreach (var letter in letters)
        {
            var start = IsImage(letter) ? letter.ImprovementCompletedOn : letter.UploadedOn;
            if (start is null || letter.ReviewFirstLockedOn is null)
            {
                missing++;
                continue;
            }

            durations.Add(NonNegative(letter.ReviewFirstLockedOn.Value - start.Value));
        }

        return ElapsedDayHistogram(durations, missing);
    }

    private static DurationChartViewModel MapImprovementDuration(
        IReadOnlyList<LetterPlanStatisticsLetterDataModel> letters)
    {
        var durations = new List<TimeSpan>();
        var missing = 0;

        foreach (var letter in letters.Where(FinishedImprovement))
        {
            if (letter.DecisionImprovementLockedOn is null || letter.ImprovementCompletedOn is null)
            {
                missing++;
                continue;
            }

            durations.Add(NonNegative(letter.ImprovementCompletedOn.Value - letter.DecisionImprovementLockedOn.Value));
        }

        return MinuteHistogram(durations, missing);
    }

    private static DurationChartViewModel MapReviewDuration(
        IReadOnlyList<LetterPlanStatisticsLetterDataModel> letters)
    {
        var durations = new List<TimeSpan>();
        var missing = 0;

        foreach (var letter in letters)
        {
            if (letter.Status is not (DocumentStatus.Approved or DocumentStatus.Rejected))
            {
                continue;
            }

            var endedOn = letter.Status == DocumentStatus.Approved ? letter.ReviewedOn : letter.RejectedOn;
            if (letter.DecisionReviewLockedOn is null || endedOn is null)
            {
                missing++;
                continue;
            }

            durations.Add(NonNegative(endedOn.Value - letter.DecisionReviewLockedOn.Value));
        }

        return MinuteHistogram(durations, missing);
    }

    private static CountChartViewModel MapRejectionsByReviewer(
        IReadOnlyList<LetterPlanStatisticsLetterDataModel> rejected)
    {
        var unattributed = rejected.Count(l => string.IsNullOrWhiteSpace(l.RejectedByName));
        var groups = rejected
            .Where(l => !string.IsNullOrWhiteSpace(l.RejectedByName))
            .GroupBy(l => l.RejectedByName!.Trim())
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key, StringComparer.Create(EsAr, ignoreCase: true))
            .ToList();

        return new CountChartViewModel
        {
            Labels = groups.Select(g => g.Key).ToArray(),
            Counts = groups.Select(g => (double)g.Count()).ToArray(),
            Note = CountNote(unattributed, "rechazo sin revisor registrado.", "rechazos sin revisor registrado."),
            HasData = groups.Count > 0,
        };
    }

    private static StackedCountChartViewModel MapReviewerActivity(
        IReadOnlyList<LetterPlanStatisticsLetterDataModel> approved,
        IReadOnlyList<LetterPlanStatisticsLetterDataModel> rejected)
    {
        var approvals = approved
            .Where(l => !string.IsNullOrWhiteSpace(l.ReviewerName))
            .GroupBy(l => l.ReviewerName!.Trim())
            .ToDictionary(g => g.Key, g => g.Count());
        var rejections = rejected
            .Where(l => !string.IsNullOrWhiteSpace(l.RejectedByName))
            .GroupBy(l => l.RejectedByName!.Trim())
            .ToDictionary(g => g.Key, g => g.Count());

        var names = approvals.Keys
            .Union(rejections.Keys)
            .OrderByDescending(name => approvals.GetValueOrDefault(name) + rejections.GetValueOrDefault(name))
            .ThenBy(name => name, StringComparer.Create(EsAr, ignoreCase: true))
            .ToList();

        var unattributed = rejected.Count(l => string.IsNullOrWhiteSpace(l.RejectedByName));

        return new StackedCountChartViewModel
        {
            Labels = names.ToArray(),
            Approvals = names.Select(name => (double)approvals.GetValueOrDefault(name)).ToArray(),
            Rejections = names.Select(name => (double)rejections.GetValueOrDefault(name)).ToArray(),
            Note = CountNote(unattributed, "rechazo sin revisor registrado.", "rechazos sin revisor registrado."),
            HasData = names.Count > 0,
        };
    }

    private static CountChartViewModel MapRejectionReasons(
        IReadOnlyList<LetterPlanStatisticsLetterDataModel> rejected)
    {
        var groups = rejected
            .GroupBy(l => string.IsNullOrWhiteSpace(l.RejectionReason) ? "Sin motivo" : l.RejectionReason.Trim())
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key, StringComparer.Create(EsAr, ignoreCase: true))
            .ToList();

        return new CountChartViewModel
        {
            Labels = groups.Select(g => g.Key).ToArray(),
            Counts = groups.Select(g => (double)g.Count()).ToArray(),
            HasData = groups.Count > 0,
        };
    }

    private static CountChartViewModel MapOutcomes(int approved, int rejected, int exemptions)
    {
        var slices = new List<(string Label, int Count)>();
        if (approved > 0)
        {
            slices.Add(("Aprobadas", approved));
        }

        if (rejected > 0)
        {
            slices.Add(("Rechazadas", rejected));
        }

        if (exemptions > 0)
        {
            slices.Add(("Eximidas", exemptions));
        }

        return new CountChartViewModel
        {
            Labels = slices.Select(s => s.Label).ToArray(),
            Counts = slices.Select(s => (double)s.Count).ToArray(),
            HasData = slices.Count > 0,
        };
    }

    private static GroupedCountChartViewModel MapScores(
        IReadOnlyList<LetterPlanStatisticsLetterDataModel> approved)
    {
        var spelling = new double[5];
        var penmanship = new double[5];
        var content = new double[5];

        foreach (var letter in approved)
        {
            CountScore(spelling, letter.SpellingScore);
            CountScore(penmanship, letter.PenmanshipScore);
            CountScore(content, letter.ContentScore);
        }

        var hasData = spelling.Concat(penmanship).Concat(content).Any(count => count > 0);
        return new GroupedCountChartViewModel
        {
            Labels = ["1", "2", "3", "4", "5"],
            Spelling = spelling,
            Penmanship = penmanship,
            Content = content,
            HasData = hasData,
        };
    }

    private static void CountScore(double[] counts, int? score)
    {
        if (score is >= 1 and <= 5)
        {
            counts[score.Value - 1]++;
        }
    }

    private static DurationChartViewModel ElapsedDayHistogram(IReadOnlyList<TimeSpan> durations, int missing) =>
        Histogram(
            DayLabels,
            durations.Select(duration => DayBucket(duration.TotalDays)),
            durations.Count == 0 ? null : ElapsedSummary(durations),
            CountNote(missing, "carta sin este dato.", "cartas sin este dato."));

    private static DurationChartViewModel MinuteHistogram(IReadOnlyList<TimeSpan> durations, int missing) =>
        Histogram(
            MinuteLabels,
            durations.Select(MinuteBucket),
            durations.Count == 0 ? null : ElapsedSummary(durations),
            CountNote(missing, "carta sin este dato.", "cartas sin este dato."));

    private static DurationChartViewModel Histogram(
        string[] labels,
        IEnumerable<int> buckets,
        DurationSummary? summary,
        string? note)
    {
        var counts = new double[labels.Length];
        foreach (var bucket in buckets)
        {
            counts[bucket]++;
        }

        return new DurationChartViewModel
        {
            Labels = labels,
            Counts = counts,
            MedianText = summary?.Median,
            AverageText = summary?.Average,
            MinText = summary?.Minimum,
            MaxText = summary?.Maximum,
            Note = note,
            HasData = counts.Any(count => count > 0),
        };
    }

    private static bool IsImage(LetterPlanStatisticsLetterDataModel letter) =>
        letter.DigitalImprovementStatus != DigitalImprovementStatus.NotApplicable;

    private static bool FinishedImprovement(LetterPlanStatisticsLetterDataModel letter) =>
        IsImage(letter)
        && (letter.ImprovementCompletedOn is not null
            || letter.DigitalImprovementStatus == DigitalImprovementStatus.Complete);

    private static int DayBucket(double days)
    {
        if (days < 1)
        {
            return 0;
        }

        if (days < 4)
        {
            return 1;
        }

        if (days < 8)
        {
            return 2;
        }

        if (days < 15)
        {
            return 3;
        }

        if (days < 31)
        {
            return 4;
        }

        return 5;
    }

    private static int MinuteBucket(TimeSpan duration)
    {
        var minutes = duration.TotalMinutes;
        if (minutes < 5)
        {
            return 0;
        }

        if (minutes <= 15)
        {
            return 1;
        }

        if (minutes <= 30)
        {
            return 2;
        }

        if (minutes <= 60)
        {
            return 3;
        }

        return 4;
    }

    private static TimeSpan NonNegative(TimeSpan duration) =>
        duration < TimeSpan.Zero ? TimeSpan.Zero : duration;

    private static TimeSpan Median(IReadOnlyList<TimeSpan> samples)
    {
        var ordered = samples.OrderBy(sample => sample).ToArray();
        var mid = ordered.Length / 2;
        if (ordered.Length % 2 == 1)
        {
            return ordered[mid];
        }

        return TimeSpan.FromTicks((ordered[mid - 1].Ticks + ordered[mid].Ticks) / 2);
    }

    private readonly record struct DurationSummary(string Median, string Average, string Minimum, string Maximum);

    private static DurationSummary CalendarSummary(IReadOnlyList<int> days)
    {
        var ordered = days.OrderBy(day => day).ToArray();
        var mid = ordered.Length / 2;
        var median = ordered.Length % 2 == 1
            ? ordered[mid]
            : (ordered[mid - 1] + ordered[mid]) / 2.0;

        return new DurationSummary(
            FormatCalendarDays("Mediana", median),
            FormatCalendarDays("Promedio", days.Average()),
            FormatCalendarDays("Mínimo", days.Min()),
            FormatCalendarDays("Máximo", days.Max()));
    }

    private static DurationSummary ElapsedSummary(IReadOnlyList<TimeSpan> durations)
    {
        var averageTicks = (long)Math.Round(durations.Average(duration => duration.Ticks));
        return new DurationSummary(
            FormatElapsed("Mediana", Median(durations)),
            FormatElapsed("Promedio", TimeSpan.FromTicks(averageTicks)),
            FormatElapsed("Mínimo", durations.Min()),
            FormatElapsed("Máximo", durations.Max()));
    }

    private static string FormatCalendarDays(string label, double days)
    {
        if (days < 1)
        {
            return $"{label}: menos de 1 día";
        }

        if (Math.Abs(days - 1) < 0.001)
        {
            return $"{label}: 1 día";
        }

        return $"{label}: {days.ToString("0.#", EsAr)} días";
    }

    private static string FormatElapsed(string label, TimeSpan duration)
    {
        if (duration.TotalDays >= 1)
        {
            var days = duration.TotalDays;
            if (days < 1.05)
            {
                return $"{label}: 1 día";
            }

            return $"{label}: {days.ToString("0.#", EsAr)} días";
        }

        if (duration.TotalHours >= 1)
        {
            var hours = (int)duration.TotalHours;
            var minutes = duration.Minutes;
            return minutes == 0
                ? $"{label}: {hours} h"
                : $"{label}: {hours} h {minutes} min";
        }

        if (duration.TotalMinutes < 1)
        {
            return $"{label}: menos de 1 min";
        }

        return $"{label}: {Math.Round(duration.TotalMinutes).ToString("0", EsAr)} min";
    }

    private static string? CountNote(int count, string singular, string plural)
    {
        if (count == 0)
        {
            return null;
        }

        return count == 1 ? $"1 {singular}" : $"{count} {plural}";
    }

    private static string? JoinNotes(string? first, string? second)
    {
        if (first is null)
        {
            return second;
        }

        if (second is null)
        {
            return first;
        }

        return $"{first} {second}";
    }
}