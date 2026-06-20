using Fonbec.Web.DataAccess.Entities.Abstract;

namespace Fonbec.Web.DataAccess.Entities;

public class PlannedDelivery : Auditable
{
    public int Id { get; set; }

    public int? ChapterId { get; set; }
    public Chapter? Chapter { get; set; }

    public DateTime StartsOn { get; set; }

    public bool Completed { get; set; }
}