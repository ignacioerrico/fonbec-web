using System;
using System.Collections.Generic;
using System.Text;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Abstract;

namespace Fonbec.Web.DataAccess.DataModels.Companies;

public class CreateCompanySponsorDataModel : UserWithoutAccount
{
    public int SponsorId { get; set; }
}


