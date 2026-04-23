using System;
using System.Collections.Generic;
using System.Text;

namespace Fonbec.Web.DataAccess.DataModels.PlannedDelivery.Input;

public class UpdatePlannedDeliveryInputDataModel
{
    public int PlannedDeliveryId { get; set; }
    public DateTime PlannedDeliveryUpdateStartsOn { get; set; }
    public bool PlannedDeliveryUpdateIsCompleted { get; set; }
    public string? PlannedDeliveryUpdateNotes { get; set; }
    public int UpdatedById { get; set; }
}





