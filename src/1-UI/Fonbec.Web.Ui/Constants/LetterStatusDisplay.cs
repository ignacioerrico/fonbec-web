using Fonbec.Web.Logic.Models.Students;
using MudBlazor;

namespace Fonbec.Web.Ui.Constants;

public static class LetterStatusDisplay
{
    public static string Icon(this LetterAggregateStatus status) => status switch
    {
        LetterAggregateStatus.Approved => Icons.Material.Filled.Check,
        LetterAggregateStatus.Pending => Icons.Material.Filled.HourglassEmpty,
        LetterAggregateStatus.Rejected => Icons.Material.Filled.Close,
        LetterAggregateStatus.Exempt => Icons.Material.Filled.RemoveCircleOutline,
        LetterAggregateStatus.NoPlan => Icons.Material.Filled.Remove,
        _ => Icons.Material.Filled.Remove,
    };

    public static Color Color(this LetterAggregateStatus status) => status switch
    {
        LetterAggregateStatus.Approved => MudBlazor.Color.Success,
        LetterAggregateStatus.Pending => MudBlazor.Color.Warning,
        LetterAggregateStatus.Rejected => MudBlazor.Color.Error,
        LetterAggregateStatus.Exempt => MudBlazor.Color.Info,
        LetterAggregateStatus.NoPlan => MudBlazor.Color.Default,
        _ => MudBlazor.Color.Default,
    };

    public static string Label(this LetterAggregateStatus status) => status switch
    {
        LetterAggregateStatus.Approved => "Aprobada",
        LetterAggregateStatus.Pending => "Pendiente",
        LetterAggregateStatus.Rejected => "Rechazada",
        LetterAggregateStatus.Exempt => "Eximido",
        LetterAggregateStatus.NoPlan => "Sin plan activo",
        _ => string.Empty,
    };

    public static string Icon(this LetterSlotStatus status) => status switch
    {
        LetterSlotStatus.Approved => Icons.Material.Filled.Check,
        LetterSlotStatus.Rejected => Icons.Material.Filled.Close,
        LetterSlotStatus.InReview => Icons.Material.Filled.HourglassEmpty,
        LetterSlotStatus.None => Icons.Material.Filled.HourglassEmpty,
        _ => Icons.Material.Filled.Remove,
    };

    public static Color Color(this LetterSlotStatus status) => status switch
    {
        LetterSlotStatus.Approved => MudBlazor.Color.Success,
        LetterSlotStatus.Rejected => MudBlazor.Color.Error,
        LetterSlotStatus.InReview or LetterSlotStatus.None => MudBlazor.Color.Warning,
        _ => MudBlazor.Color.Default,
    };

    public static string Label(this LetterSlotStatus status) => status switch
    {
        LetterSlotStatus.Approved => "Aprobada",
        LetterSlotStatus.Rejected => "Rechazada",
        LetterSlotStatus.InReview => "Pendiente (en revisión)",
        LetterSlotStatus.None => "Pendiente (sin carta)",
        _ => string.Empty,
    };
}
