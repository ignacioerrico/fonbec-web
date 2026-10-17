using Fonbec.Web.DataAccess.Entities.Enums;
using MudBlazor;

namespace Fonbec.Web.Ui.Constants;

public static class ReportCardAssessmentDisplay
{
    extension(ReportCardAssessment assessment)
    {
        public string Label() => assessment.EnumToString();

        public Color Color() => assessment switch
        {
            ReportCardAssessment.Green => MudBlazor.Color.Success,
            ReportCardAssessment.Yellow => MudBlazor.Color.Warning,
            ReportCardAssessment.Red => MudBlazor.Color.Error,
            _ => MudBlazor.Color.Default,
        };
    }
}