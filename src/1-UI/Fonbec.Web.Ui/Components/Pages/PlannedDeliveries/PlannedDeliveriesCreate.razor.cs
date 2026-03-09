using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.Models.PlannedDeliveries.Input;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Constants;
using Fonbec.Web.Ui.Models.PlannedDelivery;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.PlannedDeliveries
{
	[PageMetadata(nameof(PlannedDeliveriesCreate), "Create planned delivery", [FonbecRole.Admin])]
	public partial class PlannedDeliveriesCreate : AuthenticationRequiredComponentBase
	{
		private readonly PlannedDeliveryCreateBindModel _bindModel = new();

		private bool _formValidationSucceeded;
		public bool Completed { get; set; } = false;

		private DateTime? _minDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

		private bool _saving;
		private bool SaveButtonDisabled => Loading
								   || _saving
								   || !_formValidationSucceeded;

		[Inject]
		public IPlannedDeliveryService PlannedDeliveryService { get; set; } = null!;

		private async Task Save()
		{
			
			_saving = true;

			var createPlannedDeliveryInputModel = new CreatePlannedDeliveryInputModel(
				_bindModel.StartsOn,
				_bindModel.Completed,
				_bindModel.ChapterId,
				FonbecClaim.UserId);

			var result = await PlannedDeliveryService.CreatePlannedDeliveryAsync(createPlannedDeliveryInputModel);
			if (!result.AnyAffectedRows)
			{
				Snackbar.Add(result.Message ?? "Save failed", Severity.Error);
				_saving = false;
				return;
			}
			
			NavigationManager.NavigateTo(NavRoutes.PlannedDeliveries);
		}
	}
}

