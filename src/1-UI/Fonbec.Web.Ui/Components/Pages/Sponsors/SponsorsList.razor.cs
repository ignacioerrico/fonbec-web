using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models.Chapters.Input;
using Fonbec.Web.Logic.Models.Chapters;
using Fonbec.Web.Logic.Models.Sponsors;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Fonbec.Web.Logic.Models.Students;

namespace Fonbec.Web.Ui.Components.Pages.Sponsors;

[PageMetadata(nameof(SponsorsList), "Lista de padrinos", [FonbecRole.Manager, FonbecRole.Admin])]

public partial class SponsorsList : AuthenticationRequiredComponentBase
{
    private List<SponsorsListViewModel> _viewModels = [];
    private string _searchString = string.Empty;
    [Inject]
    public ISponsorService SponsorService { get; set; } = null!;

    private bool _sortByLastName;
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        Loading = true;
        _viewModels = await SponsorService.GetAllSponsorsAsync();
        Loading = false;
    }

    private bool Filter(SponsorsListViewModel viewModel) =>
        string.IsNullOrWhiteSpace(_searchString)
        || $"{viewModel.SponsorFirstName} {viewModel.SponsorLastName}".ContainsIgnoringAccents(_searchString)
        || (!string.IsNullOrWhiteSpace(viewModel.SponsorNickName)
            && $"{viewModel.SponsorNickName} {viewModel.SponsorLastName}".ContainsIgnoringAccents(_searchString))
        || (!string.IsNullOrWhiteSpace(viewModel.SponsorEmail)
            && viewModel.SponsorEmail.ContainsIgnoringAccents(_searchString))
        || (!string.IsNullOrWhiteSpace(viewModel.SponsorPhoneNumber)
            && viewModel.SponsorPhoneNumber.ContainsIgnoringAccents(_searchString));

    private async Task CommittedItemChangesAsync(SponsorsListViewModel viewModel)
    {
    }

    private string SponsorFullName(SponsorsListViewModel viewModel) =>
        _sortByLastName
            ? $"{viewModel.SponsorLastName}, {viewModel.SponsorFirstName}"
            : $"{viewModel.SponsorFirstName} {viewModel.SponsorLastName}";
}