using System;
using System.Collections.Generic;
using System.Text;

namespace Fonbec.Web.DataAccess.DataModels.Companies.Input;

public class CreateCompanyInputDataModel
{
    public string CompanyName { get; set; } = null!; 

    public string? CompanyEmail { get; set; }

    public string? CompanyPhoneNumber { get; set; }

    public List<int> CompanySponsorsIds { get; set; } = [];
    public int CreatedById { get; set; }

}
