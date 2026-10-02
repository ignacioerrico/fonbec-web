namespace Fonbec.Web.DataAccess.DataModels.Sponsors;

public class SponsoredStudentDataModel
{
    public string Name { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }
}