using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.Models.Sponsorships;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.Ui.Components.Pages.Sponsorships;

[PageMetadata(nameof(SponsorshipsList), "Padrinos del becario", [FonbecRole.Manager])]

public partial class SponsorshipsList : AuthenticationRequiredComponentBase
{
    private List<SponsorshipsListViewModel> _viewModels = [];

    private string StudentFullName = "";

    [Inject]
    public ISponsorshipService SponsorshipService { get; set; } = null!;

    [Parameter]
    public int StudentId { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        Loading = true;

        _viewModels = await SponsorshipService.GetAllSponsorshipsAsync(StudentId);

        StudentFullName = _viewModels.FirstOrDefault()?.StudentFullName ?? "";

        Loading = false;
    }

}