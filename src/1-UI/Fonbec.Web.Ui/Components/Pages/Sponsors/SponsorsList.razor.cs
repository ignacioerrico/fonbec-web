using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models.Sponsors;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.Pages.Sponsors;

[PageMetadata(nameof(SponsorsList), "Lista de padrinos", [FonbecRole.Manager])]
public partial class SponsorsList : AuthenticationRequiredComponentBase
{
    private List<SponsorsListViewModel> _viewModels = [];

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
    }

    /// <summary>
    /// Called by MudTable to determine if a row should be displayed
    /// </summary>
    /// <param name="viewModel">The sponsor view model to evaluate against the search string. Cannot be null.</param>
    /// <returns>true if the student matches the search string in any of the relevant fields; otherwise, false.</returns>
    private bool Filter(SponsorsListViewModel viewModel) =>
        string.IsNullOrWhiteSpace(_searchString)
        || $"{viewModel.SponsorFirstName} {viewModel.SponsorLastName}".ContainsIgnoringAccents(_searchString);

    private string SponsorFullName(SponsorsListViewModel viewModel) =>
    _sortByLastName
        ? $"{viewModel.SponsorLastName}, {viewModel.SponsorFirstName}"
        : $"{viewModel.SponsorFirstName} {viewModel.SponsorLastName}";
}