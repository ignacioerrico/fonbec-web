using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.DataAccess.DataModels.Students;

public class StudentsFilterModel
{
    public IEnumerable<int>? FacilitatorIds { get; set; }
    public EducationLevel? EducationLevel { get; set; }
    public string? Search { get; set; }
}
