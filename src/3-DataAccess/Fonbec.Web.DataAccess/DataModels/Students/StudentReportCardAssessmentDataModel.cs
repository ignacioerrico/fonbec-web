using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.DataAccess.DataModels.Students;

public class StudentReportCardAssessmentDataModel
{
    public int StudentId { get; init; }

    public DateOnly Period { get; init; }

    public string Description { get; init; } = string.Empty;

    public ReportCardAssessment Assessment { get; init; }
}