namespace Fonbec.Web.DataAccess.DataModels.PlannedDelivery;

public class CurrentPlannedDeliveryDataModel
{
    public int PlannedDeliveryId { get; init; }
    public DateTime PlannedDeliveryStartsOn { get; init; }
    public bool IsPlannedDeliveryCompleted { get; init; }
}