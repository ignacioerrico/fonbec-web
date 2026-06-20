namespace Fonbec.Web.Ui.Models.PlannedDelivery;

public class PlannedDeliveryCreateBindModel
{
    public DateTime? PlanStartsOn { get; set; }
    public string PlanNotes { get; set; } = string.Empty;
}