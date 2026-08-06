using Fonbec.Web.Logic.Models.Students;
using MudBlazor;

namespace Fonbec.Web.Ui.Constants;

public static class LetterStatusDisplay
{
    public static string Icon(this LetterAggregateStatus status) => status switch
    {
        LetterAggregateStatus.NotUploaded => Icons.Material.Filled.UploadFile,
        LetterAggregateStatus.Partial => Icons.Material.Filled.Warning,
        LetterAggregateStatus.Complete => Icons.Material.Filled.CheckCircle,
        LetterAggregateStatus.Exempt => Icons.Material.Filled.RemoveCircleOutline,
        LetterAggregateStatus.NoPlan => Icons.Material.Filled.Remove,
        _ => Icons.Material.Filled.Remove,
    };

    public static Color Color(this LetterAggregateStatus status) => status switch
    {
        LetterAggregateStatus.NotUploaded => MudBlazor.Color.Error,
        LetterAggregateStatus.Partial => MudBlazor.Color.Warning,
        LetterAggregateStatus.Complete => MudBlazor.Color.Success,
        LetterAggregateStatus.Exempt => MudBlazor.Color.Info,
        LetterAggregateStatus.NoPlan => MudBlazor.Color.Default,
        _ => MudBlazor.Color.Default,
    };

    public static string Label(this LetterAggregateStatus status) => status switch
    {
        LetterAggregateStatus.NotUploaded => "Falta subir",
        LetterAggregateStatus.Partial => "Faltan cartas",
        LetterAggregateStatus.Complete => "Carta subida",
        LetterAggregateStatus.Exempt => "Eximido",
        LetterAggregateStatus.NoPlan => "Sin campaña activa",
        _ => string.Empty,
    };

    public static string Icon(this LetterSlotStatus status) => status switch
    {
        LetterSlotStatus.None => Icons.Material.Filled.UploadFile,
        LetterSlotStatus.InReview => Icons.Material.Filled.HourglassTop,
        LetterSlotStatus.Approved => Icons.Material.Filled.CheckCircle,
        LetterSlotStatus.Rejected => Icons.Material.Filled.Replay,
        _ => Icons.Material.Filled.Remove,
    };

    public static Color Color(this LetterSlotStatus status) => status switch
    {
        LetterSlotStatus.None => MudBlazor.Color.Error,
        LetterSlotStatus.InReview => MudBlazor.Color.Info,
        LetterSlotStatus.Approved => MudBlazor.Color.Success,
        LetterSlotStatus.Rejected => MudBlazor.Color.Error,
        _ => MudBlazor.Color.Default,
    };

    public static string Label(this LetterSlotStatus status) => status switch
    {
        LetterSlotStatus.None => "Falta subir",
        LetterSlotStatus.InReview => "Esperando aprobación",
        LetterSlotStatus.Approved => "Aprobada",
        LetterSlotStatus.Rejected => "Rechazada",
        _ => string.Empty,
    };
}