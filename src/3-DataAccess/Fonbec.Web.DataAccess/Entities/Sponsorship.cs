using Fonbec.Web.DataAccess.Entities.Abstract;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fonbec.Web.DataAccess.Entities;

public class Sponsorship : Auditable
{
    // pk
    public int SponsorshipId { get; set; }
    public int StudentId { get; set; }  
    public int SponsorId { get; set; }
    public Student Student { get; set; } = null!;
    public Sponsor Sponsor { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
