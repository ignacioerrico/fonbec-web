using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models.Sponsors;
using Fonbec.Web.Logic.Models.Sponsors.Input;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Sponsors;

[PageMetadata(nameof(SponsorsList), "Lista de padrinos", [FonbecRole.Admin, FonbecRole.Manager])]
public partial class SponsorsList : AuthenticationRequiredComponentBase
{
    private List<SponsorsListViewModel> _viewModels = [];
    private SponsorsListViewModel _originalViewModel = new();

    private IEnumerable<string> _allCompanyNames = [];
    private IEnumerable<string> _allChapters = [];

    private string _searchString = string.Empty;

    private bool _sortByLastName;

    [Inject]
    public ISponsorService SponsorService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        Loading = true;

        _viewModels = await SponsorService.GetAllSponsorsAsync(FonbecClaim.ChapterId);

        Loading = false;

        _allCompanyNames = _viewModels
            .Select(vm => vm.SponsorCompanyName)
            .Distinct()
            .OrderBy(name => name);

        _allChapters = _viewModels
            .Select(vm => vm.SponsorChapterName)
            .Distinct()
            .OrderBy(name => name);
    }

    /// <summary>
    /// Called by MudTable to determine if a row should be displayed
    /// </summary>
    /// <param name="viewModel">The sponsor view model to evaluate against the search string. Cannot be null.</param>
    /// <returns>true if the student matches the search string in any of the relevant fields; otherwise, false.</returns>
    private bool Filter(SponsorsListViewModel viewModel) =>
        string.IsNullOrWhiteSpace(_searchString)
        || $"{viewModel.SponsorFirstName} {viewModel.SponsorLastName}".ContainsIgnoringAccents(_searchString)
        || (!string.IsNullOrEmpty(viewModel.SponsorNickName)
            && $"{viewModel.SponsorNickName} {viewModel.SponsorLastName}".ContainsIgnoringAccents(_searchString))
        || viewModel.SponsorEmail.Contains(_searchString, StringComparison.OrdinalIgnoreCase)
        || (!string.IsNullOrEmpty(viewModel.SponsorPhoneNumber)
            && viewModel.SponsorPhoneNumber.ContainsIgnoringSpaces(_searchString));

    private string SponsorFullName(SponsorsListViewModel viewModel) =>
    _sortByLastName
        ? $"{viewModel.SponsorLastName}, {viewModel.SponsorFirstName}"
        : $"{viewModel.SponsorFirstName} {viewModel.SponsorLastName}";

    private void StartedEditingItem(SponsorsListViewModel originalViewModel) =>
       _originalViewModel = originalViewModel.DeepClone();

    private async Task CommittedItemChangesAsync(SponsorsListViewModel modifiedViewModel)
    {
        if (_originalViewModel.IsEqualTo(modifiedViewModel))
        {
            Snackbar.Add("No se realizaron cambios.", Severity.Info);
            return;
        }

        var updateSponsorInputModel = new UpdateSponsorInputModel(
            modifiedViewModel.SponsorId,
            modifiedViewModel.SponsorFirstName,
            modifiedViewModel.SponsorLastName,
            modifiedViewModel.SponsorNickName,
            modifiedViewModel.SponsorGender,
            modifiedViewModel.SponsorPhoneNumber,
            modifiedViewModel.SponsorEmail,
            modifiedViewModel.SponsorCompanyId,
            FonbecClaim.UserId
        );

        Loading = true;

        var result = await SponsorService.UpdateSponsorAsync(updateSponsorInputModel);

        Loading = false;

        if (result.AnyAffectedRows)
        {
            // Update timestamp in UI
            _viewModels.Single(vm => vm.SponsorId == modifiedViewModel.SponsorId).LastUpdatedOnUtc = DateTime.Now;
            Snackbar.Add("Padrino actualizado correctamente.", Severity.Success);
        }
        else
        {
            Snackbar.Add("No se pudo actualizar el padrino.", Severity.Error);
        }
    }
}