using Fonbec.Web.DataAccess.Entities.Abstract;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fonbec.Web.DataAccess.DataModels.PlannedDelivery;
public class AllPlannedDeliveriesDataModel(Auditable auditable) : AuditableDataModel(auditable)
{
    public int PlanId { get; init; }
    public DateTime PlanStartsOn { get; set; }

}

