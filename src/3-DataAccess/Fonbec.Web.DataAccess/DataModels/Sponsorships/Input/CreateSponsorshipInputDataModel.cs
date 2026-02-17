using System;
using System.Collections.Generic;
using System.Text;

namespace Fonbec.Web.DataAccess.DataModels.Sponsorships.Input;

public class CreateSponsorshipInputDataModel
{ 
    public int StudentId { get; set; }
    public int SponsorId { get; set; }
    public DateTime SponsorshipStartDate { get; set; }
    public DateTime? SponsorshipEndDate { get; set; }
    public int CreatedById { get; set; }
}
