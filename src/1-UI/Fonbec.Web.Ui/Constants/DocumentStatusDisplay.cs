using Fonbec.Web.DataAccess.Entities.Enums;
using MudBlazor;

namespace Fonbec.Web.Ui.Constants;

public static class DocumentStatusDisplay
{
    extension(DocumentStatus status)
    {
        public string Label() => status switch
        {
            DocumentStatus.DigitalImprovementPending => "Pendiente de mejora",
            DocumentStatus.DigitalImprovementOngoing => "Procesando mejora",
            DocumentStatus.ReviewPending => "Pendiente",
            DocumentStatus.ReviewOngoing => "Procesando",
            DocumentStatus.Approved => "Aprobado",
            DocumentStatus.Rejected => "Rechazado",
            _ => status.ToString()
        };

        public string Icon() => status switch
        {
            DocumentStatus.DigitalImprovementPending => Icons.Material.Filled.Build,
            DocumentStatus.DigitalImprovementOngoing => Icons.Material.Filled.BuildCircle,
            DocumentStatus.ReviewPending => Icons.Material.Filled.HourglassEmpty,
            DocumentStatus.ReviewOngoing => Icons.Material.Filled.Autorenew,
            DocumentStatus.Approved => Icons.Material.Filled.CheckCircle,
            DocumentStatus.Rejected => Icons.Material.Filled.Cancel,
            _ => Icons.Material.Filled.Remove,
        };

        public Color Color() => status switch
        {
            DocumentStatus.DigitalImprovementPending => MudBlazor.Color.Warning,
            DocumentStatus.DigitalImprovementOngoing => MudBlazor.Color.Info,
            DocumentStatus.ReviewPending => MudBlazor.Color.Warning,
            DocumentStatus.ReviewOngoing => MudBlazor.Color.Info,
            DocumentStatus.Approved => MudBlazor.Color.Success,
            DocumentStatus.Rejected => MudBlazor.Color.Error,
            _ => MudBlazor.Color.Default,
        };
    }
}