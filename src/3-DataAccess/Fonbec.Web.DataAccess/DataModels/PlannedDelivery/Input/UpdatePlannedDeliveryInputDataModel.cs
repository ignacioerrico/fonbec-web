namespace Fonbec.Web.DataAccess.DataModels.PlannedDelivery.Input;

public class UpdatePlannedDeliveryInputDataModel
{
    public int PlannedDeliveryId { get; set; }
    public DateTime PlannedDeliveryStartsOn { get; set; }
    public string? PlannedDeliveryNotes { get; set; }
    public int UpdatedById { get; set; }
}