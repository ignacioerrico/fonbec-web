namespace Fonbec.Web.Ui.Models.PlannedDelivery
{
	public class PlannedDeliveryCreateBindModel
	{
		public DateTime? StartsOn { get; set; } 
		public bool Completed { get; set; } 
		public string? PlannedDeliveryNotes { get; set; }
		
	}
}
