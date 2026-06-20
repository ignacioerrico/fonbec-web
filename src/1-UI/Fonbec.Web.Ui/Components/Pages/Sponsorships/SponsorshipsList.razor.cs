using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.Models.Sponsorships;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.Pages.Sponsorships;

[PageMetadata(nameof(SponsorshipsList), "Lista de padrinos de un becario", [FonbecRole.Manager])]

public partial class SponsorshipsList : AuthenticationRequiredComponentBase
{
    private SponsorshipsListViewModel _viewModel = new();

    [Inject]
    public ISponsorshipService SponsorshipService { get; set; } = null!;

    [Parameter]
    public int StudentId { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        Loading = true;

        _viewModel = await SponsorshipService.GetAllSponsorshipsAsync(StudentId);

        Loading = false;
    }
}