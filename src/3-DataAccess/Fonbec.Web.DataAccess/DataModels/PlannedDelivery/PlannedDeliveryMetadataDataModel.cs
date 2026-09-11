namespace Fonbec.Web.DataAccess.DataModels.PlannedDelivery;

public class PlannedDeliveryMetadataDataModel
{
    public int PlannedDeliveryId { get; init; }
    public int? ChapterId { get; init; }
    public DateTime StartsOn { get; init; }
    public bool Completed { get; init; }
}