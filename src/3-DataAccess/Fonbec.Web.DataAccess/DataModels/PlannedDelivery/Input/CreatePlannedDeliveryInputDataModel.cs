namespace Fonbec.Web.DataAccess.DataModels.PlannedDelivery.Input;

public class CreatePlannedDeliveryInputDataModel
{
    public int ChapterId { get; set; }
    public DateTime PlanStartsOn { get; set; }
    public string? PlanNotes { get; set; }
    public int CreatedById { get; set; }
}