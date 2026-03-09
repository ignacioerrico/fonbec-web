namespace Fonbec.Web.Ui.Models.PlannedDelivery
{
	public class PlannedDeliveryCreateBindModel
	{
		public int ChapterId { get; set; }
		public DateTime? StartsOn { get; set; } 
		public bool Completed { get; set; }
		
	}
}
