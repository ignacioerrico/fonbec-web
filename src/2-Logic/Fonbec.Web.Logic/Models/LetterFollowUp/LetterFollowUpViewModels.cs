using Fonbec.Web.DataAccess.Entities.Enums;
using System.Globalization;

namespace Fonbec.Web.Logic.Models.LetterFollowUp;

public enum LetterFollowUpTaskKind
{
    RedFlag,
    GreenFlag,
}

public sealed class LetterFollowUpViewModel
{
    public List<LetterFollowUpTaskViewModel> RedFlags { get; set; } = [];

    public List<LetterFollowUpTaskViewModel> GreenFlags { get; set; } = [];
}

public sealed class LetterFollowUpTaskViewModel
{
    public long AssessmentId { get; set; }

    public LetterFollowUpTaskKind Kind { get; set; }

    public string StudentFullName { get; set; } = string.Empty;

    public string FacilitatorFullName { get; set; } = string.Empty;

    public string FacilitatorEmail { get; set; } = string.Empty;

    public string ReviewerFullName { get; set; } = string.Empty;

    public string ReviewerEmail { get; set; } = string.Empty;

    public DateTime ReportedOn { get; set; }

    public string ReportedOnText =>
        ReportedOn.ToString(@"d \d\e MMMM \d\e yyyy", new CultureInfo("es-AR"));

    public string Comment { get; set; } = string.Empty;

    public RedFlagPriority? Priority { get; set; }
}

public static class RedFlagPriorityExtensions
{
    public static string ToSpanishLabel(this RedFlagPriority priority) => priority switch
    {
        RedFlagPriority.High => "Alta",
        RedFlagPriority.Medium => "Media",
        RedFlagPriority.Low => "Baja",
        _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, null),
    };
}