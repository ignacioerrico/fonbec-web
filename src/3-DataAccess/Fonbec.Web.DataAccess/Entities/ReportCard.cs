namespace Fonbec.Web.DataAccess.Entities;

public class ReportCard : Document
{
    /// <summary>
    /// Month/year the report card corresponds to (day component is ignored).
    /// </summary>
    public DateOnly Period { get; set; }

    public string Description { get; set; } = string.Empty;

    public ReportCardReview? Review { get; set; }
}