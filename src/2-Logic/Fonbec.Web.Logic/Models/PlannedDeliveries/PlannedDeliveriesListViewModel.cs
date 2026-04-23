using Fonbec.Web.DataAccess.DataModels.PlannedDelivery;
using Fonbec.Web.Logic.ExtensionMethods;
using Mapster;

namespace Fonbec.Web.Logic.Models.PlannedDeliveries;

public class PlannedDeliveriesListViewModel : AuditableViewModel, IDetectChanges<PlannedDeliveriesListViewModel>
{
    public int PlannedDeliveryId { get; init; }
    public DateTime PlanStartsOn { get; set; }
    public bool IsPlannedDeliveryCompleted { get; set; }
    public bool IsIdenticalTo(PlannedDeliveriesListViewModel other) =>
        PlanStartsOn == other.PlanStartsOn
        && IsPlannedDeliveryCompleted == other.IsPlannedDeliveryCompleted;

    public string StatusText => IsPlannedDeliveryCompleted ? "Completed" : "Not completed";
    public DateOnly PlanStartsOnDate =>
    new DateOnly(PlanStartsOn.Year, PlanStartsOn.Month, PlanStartsOn.Day);
    public string PlanStartsOnText => PlanStartsOn.ToString("MMMM, yyyy");

}

public class PlannedDeliveriesListViewModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<AllPlannedDeliveriesDataModel, PlannedDeliveriesListViewModel>()
            .Map(dest => dest.PlannedDeliveryId, src => src.PlanId)
            .Map(dest => dest.PlanStartsOn, src => src.PlanStartsOn);

        config.NewConfig<PlannedDeliveriesListViewModel, SelectableModel<DateTime>>()
            .Map(dest => dest.Key, src => src.PlanStartsOn)
            .Map(dest => dest.DisplayName, src => src.PlanStartsOn.ToString("yyyy-MM"));
    }
}

