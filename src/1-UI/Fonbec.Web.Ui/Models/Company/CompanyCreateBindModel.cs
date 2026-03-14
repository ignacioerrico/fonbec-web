namespace Fonbec.Web.Ui.Models.Company;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.Logic.Models.Companies;
using Fonbec.Web.Logic.Models.Sponsors;

public class CompanyCreateBindModel
{
    public string CompanyName { get; set; } = null!;

    public string CompanyEmail { get; set; } = null!;

    public string CompanyPhoneNumber { get; set; } = null!;

    public List<CreateCompanySponsorsListViewModel> CompanySponsors { get; set; } = [];

}

