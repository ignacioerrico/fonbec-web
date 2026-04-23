using System;
using System.Collections.Generic;
using System.Text;

namespace Fonbec.Web.DataAccess.DataModels.PlannedDelivery.Input;

public class UpdatePlannedDeliveryInputDataModel
{
    public int PlannedDeliveryId { get; set; }
    public DateTime PlanStartsOn { get; set; }
    public string? PlanNotes { get; set; }
    public int UpdatedById { get; set; }
}





