using Fonbec.Web.DataAccess.Entities.Enums;
using MudBlazor;

public static class DocumentStatusDisplay
{
    public static string Label(this DocumentStatus status) =>
        status switch
        {
            DocumentStatus.Pending => "Pendiente",
            DocumentStatus.PendingImprovement => "Pendiente de mejora",
            DocumentStatus.ProcessingImprovement => "Procesando mejora",
            DocumentStatus.Processing => "Procesando",
            DocumentStatus.Approved => "Aprobado",
            DocumentStatus.Rejected => "Rechazado",
            _ => status.ToString()
        };

    public static string Icon(this DocumentStatus status) => status switch
    {
        DocumentStatus.Pending => Icons.Material.Filled.HourglassEmpty,
        DocumentStatus.PendingImprovement => Icons.Material.Filled.Build,
        DocumentStatus.ProcessingImprovement => Icons.Material.Filled.BuildCircle,
        DocumentStatus.Processing => Icons.Material.Filled.Autorenew,
        DocumentStatus.Approved => Icons.Material.Filled.CheckCircle,
        DocumentStatus.Rejected => Icons.Material.Filled.Cancel,
        _ => Icons.Material.Filled.Remove,
    };

    public static Color Color(this DocumentStatus status) => status switch
    {
        DocumentStatus.Pending => MudBlazor.Color.Warning,
        DocumentStatus.PendingImprovement => MudBlazor.Color.Warning,
        DocumentStatus.ProcessingImprovement => MudBlazor.Color.Info,
        DocumentStatus.Processing => MudBlazor.Color.Info,
        DocumentStatus.Approved => MudBlazor.Color.Success,
        DocumentStatus.Rejected => MudBlazor.Color.Error,
        _ => MudBlazor.Color.Default,
    };
}