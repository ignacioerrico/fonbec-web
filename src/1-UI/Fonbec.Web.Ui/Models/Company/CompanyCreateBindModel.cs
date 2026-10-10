using Fonbec.Web.Logic.Models;

namespace Fonbec.Web.Ui.Models.Company;

public class CompanyCreateBindModel
{
    public string CompanyName { get; set; } = null!;

    public string CompanyEmail { get; set; } = string.Empty;

    public string CompanyPhoneNumber { get; set; } = string.Empty;

    public string CompanyNotes { get; set; } = string.Empty;

    public List<CompanyCreatePointOfContactBindModel> PointsOfContact { get; set; } = [];

    public List<SelectableModel<int>> Sponsors { get; set; } = [];
}

public class CompanyCreatePointOfContactBindModel
{
    public Guid TempId { get; set; } = Guid.NewGuid();
    public string PocFirstName { get; set; } = string.Empty;
    public string PocLastName { get; set; } = string.Empty;
    public string PocNickName { get; set; } = string.Empty;
    public string PocEmail { get; set; } = string.Empty;
    public string PocPhoneNumber { get; set; } = string.Empty;
    public string PocNotes { get; set; } = string.Empty;
}