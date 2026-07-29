namespace Fonbec.Web.DataAccess.DataModels.Students;

/// <summary>An active sponsor (person or company) of a student, for the students list.</summary>
public class StudentActiveSponsorDataModel
{
    public string Name { get; set; } = string.Empty;

    public bool IsCompany { get; set; }
}
