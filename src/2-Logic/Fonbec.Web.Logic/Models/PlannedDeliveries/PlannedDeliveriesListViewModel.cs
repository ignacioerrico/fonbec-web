using Fonbec.Web.DataAccess.DataModels.PlannedDelivery;
using Fonbec.Web.Logic.ExtensionMethods;
using Mapster;
using System.Globalization;

namespace Fonbec.Web.Logic.Models.PlannedDeliveries;

public class PlannedDeliveriesListViewModel : AuditableViewModel, IDetectChanges<PlannedDeliveriesListViewModel>
{
    public int PlannedDeliveryId { get; init; }
    public DateTime PlannedDeliveryStartsOn { get; set; }
    public bool IsPlannedDeliveryCompleted { get; set; }

    public string PlannedDeliveryStartsOnText => PlannedDeliveryStartsOn.ToString(@"MMMM \d\e yyyy", new CultureInfo("es-AR"));

    public string PlannedDeliveryStatusText => IsPlannedDeliveryCompleted ? "Completada" : "En curso";

    public bool IsIdenticalTo(PlannedDeliveriesListViewModel other) =>
        PlannedDeliveryStartsOn == other.PlannedDeliveryStartsOn
        && Notes == other.Notes;
}

public class PlannedDeliveriesListViewModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<AllPlannedDeliveriesDataModel, PlannedDeliveriesListViewModel>()
            .Map(dest => dest.PlannedDeliveryId, src => src.PlannedDeliveryId)
            .Map(dest => dest.PlannedDeliveryStartsOn, src => src.PlannedDeliveryStartsOn)
            .Map(dest => dest.IsPlannedDeliveryCompleted, src => src.IsPlannedDeliveryCompleted);
    }
}