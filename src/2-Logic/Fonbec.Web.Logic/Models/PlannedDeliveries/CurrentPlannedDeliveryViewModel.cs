using System.Globalization;
using Fonbec.Web.DataAccess.DataModels.PlannedDelivery;
using Mapster;

namespace Fonbec.Web.Logic.Models.PlannedDeliveries;

public class CurrentPlannedDeliveryViewModel
{
    public int PlannedDeliveryId { get; init; }
    public DateTime PlannedDeliveryStartsOn { get; init; }
    public bool IsPlannedDeliveryCompleted { get; init; }

    public string PlannedDeliveryStartsOnText =>
        PlannedDeliveryStartsOn.ToString(@"MMMM \d\e yyyy", new CultureInfo("es-AR"));
}

public class CurrentPlannedDeliveryViewModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CurrentPlannedDeliveryDataModel, CurrentPlannedDeliveryViewModel>();
    }
}