using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models.Sponsors;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
namespace Fonbec.Web.Ui.Components.Pages.Sponsors;

// agregamos este atributo

[PageMetadata(nameof(SponsorsList), "Lista de Sponsors", [FonbecRole.Manager])]

public partial class SponsorsList : AuthenticationRequiredComponentBase
{
    private List<SponsorsListViewModel> _viewModels = [];

    // this...
    private string _searchString = string.Empty;
    private bool _sortByLastName;

    // dependency injection
    [Inject]
    public ISponsorService SponsorService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    { 
        await base.OnInitializedAsync();

        // this is to prevent Blazor from blocking the render process due to this asynchronous statement
        Loading = true;

        // paso la claim...
        _viewModels = await SponsorService.GetAllSponsorsAsync(FonbecClaim.ChapterId);

        Loading = false;
    }

    /// <summary>
    /// Called by MudTable to determine if a row should be displayed
    /// </summary>
    /// <param name="viewModel">The sponsors view model to evaluate against the search string. Cannot be null.</param>
    /// <returns>true if the student matches the search string in any of the relevant fields; otherwise, false.</returns>
    private bool Filter(SponsorsListViewModel viewModel) =>
        string.IsNullOrWhiteSpace(_searchString)
        || $"{viewModel.SponsorFirstName} {viewModel.SponsorLastName}".ContainsIgnoringAccents(_searchString);

    private string SponsorFullName(SponsorsListViewModel viewModel) =>
    _sortByLastName
        ? $"{viewModel.SponsorFirstName} {viewModel.SponsorLastName}"
        : $"{viewModel.SponsorLastName}, {viewModel.SponsorFirstName}";
}
