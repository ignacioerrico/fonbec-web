
using Fonbec.Web.DataAccess.Constants;

namespace Fonbec.Web.Ui.Components.Pages.PlannedDeliveries
{
	[PageMetadata(nameof(PlannedDeliveriesList), "List of planned deliveries", [FonbecRole.Admin, FonbecRole.Manager])]
	public partial class PlannedDeliveriesList : AuthenticationRequiredComponentBase
	{
	}
}
