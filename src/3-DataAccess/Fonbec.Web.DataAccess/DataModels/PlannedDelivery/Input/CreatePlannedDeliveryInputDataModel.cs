using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fonbec.Web.DataAccess.DataModels.PlannedDelivery.Input;

	public class CreatePlannedDeliveryInputDataModel
	{
		public int ChapterId { get; set; }
		public DateTime DeliverableStartsOn { get; set; }
		public bool IsCompleted { get; set; }
		public string? PlannedDeliveryNotes { get; set; }
		public int CreatedById { get; set; }
}

	
