using System;
using System.Collections.Generic;
using System.Text;

namespace Fonbec.Web.DataAccess.DataModels.Companies.Input;

public class UpdateCompanyInputDataModel
{
    public int CompanyId { get; set; }

    public string CompanyUpdatedName { get; set; } = null!;

    public string? CompanyUpdatedPhoneNumber { get; set; }

    public string? CompanyUpdatedEmail { get; set; }
    public string? CompanyUpdatedNotes { get; set; }

    public int UpdatedById { get; set; }
}