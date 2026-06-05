using Fonbec.Web.DataAccess.Entities.Abstract;

namespace Fonbec.Web.DataAccess.DataModels.PlannedDelivery;

public class AllPlannedDeliveriesDataModel(Auditable auditable) : AuditableDataModel(auditable)
{
    public int PlannedDeliveryId { get; init; }
    public DateTime PlannedDeliveryStartsOn { get; set; }
    public bool IsPlannedDeliveryCompleted { get; set; }
}