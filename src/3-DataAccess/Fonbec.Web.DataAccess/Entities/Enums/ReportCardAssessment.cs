namespace Fonbec.Web.DataAccess.Entities.Enums;

public enum ReportCardAssessment : byte
{
    Green = 1,
    Yellow = 2,
    Red = 3,
}

public static class ReportCardAssessmentTranslator
{
    public static string EnumToString(this ReportCardAssessment assessment) =>
        assessment switch
        {
            ReportCardAssessment.Green => "Verde",
            ReportCardAssessment.Yellow => "Amarillo",
            ReportCardAssessment.Red => "Rojo",
            _ => "(Desconocido)",
        };
}