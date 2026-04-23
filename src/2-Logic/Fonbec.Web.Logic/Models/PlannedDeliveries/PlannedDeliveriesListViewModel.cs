using Fonbec.Web.DataAccess.DataModels.PlannedDelivery;
using Fonbec.Web.Logic.ExtensionMethods;
using Mapster;

namespace Fonbec.Web.Logic.Models.PlannedDeliveries;

public class PlannedDeliveriesListViewModel : AuditableViewModel, IDetectChanges<PlannedDeliveriesListViewModel>
{
    public int PlannedDeliveryId { get; init; }
    public DateTime PlannedDeliveryStartsOn { get; set; }
    public bool IsPlannedDeliveryCompleted { get; set; }
    public bool IsIdenticalTo(PlannedDeliveriesListViewModel other) =>
        PlannedDeliveryStartsOn == other.PlannedDeliveryStartsOn
        && IsPlannedDeliveryCompleted == other.IsPlannedDeliveryCompleted;

    public string StatusText => IsPlannedDeliveryCompleted ? "Completed" : "Not completed";
    public string PlannedDeliveryStartsOnText => PlannedDeliveryStartsOn.ToString("MMMM, yyyy");
    public DateOnly PlannedDeliveryMonthYearDate =>
    new DateOnly(PlannedDeliveryStartsOn.Year, PlannedDeliveryStartsOn.Month, PlannedDeliveryStartsOn.Day);

}

public class PlannedDeliveriesListViewModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<AllPlannedDeliveriesDataModel, PlannedDeliveriesListViewModel>()
            .Map(dest => dest.PlannedDeliveryId, src => src.PlannedDeliveryId)
            .Map(dest => dest.PlannedDeliveryStartsOn, src => src.PlannedDeliveryStartsOn)
            .Map(dest => dest.IsPlannedDeliveryCompleted, src => src.IsPlannedDeliveryCompleted);

        config.NewConfig<PlannedDeliveriesListViewModel, SelectableModel<DateTime>>()
            .Map(dest => dest.Key, src => src.PlannedDeliveryStartsOn)
            .Map(dest => dest.DisplayName, src => src.PlannedDeliveryStartsOn.ToString("yyyy-MM-dd"));
    }
}

