using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.Models.Sponsorships;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Components.NonPages.Dialogs;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Sponsorships;

[PageMetadata(nameof(SponsorshipsList), "Lista de padrinos de un becario", [FonbecRole.Manager])]

public partial class SponsorshipsList : AuthenticationRequiredComponentBase
{
    private SponsorshipsListViewModel _viewModel = new();

    [Inject]
    public ISponsorshipService SponsorshipService { get; set; } = null!;

    [Inject]
    public IDialogService DialogService { get; set; } = null!;

    [Parameter]
    public int StudentId { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        Loading = true;
        _viewModel = await SponsorshipService.GetAllSponsorshipsAsync(StudentId);
        Loading = false;
    }

    private async Task OpenEditDialogAsync(SponsorshipsSponsorshipsListViewModel sponsorship)
    {
        var parameters = new DialogParameters<SponsorshipEditDialog>
        {
            { x => x.Sponsorship, sponsorship },
            { x => x.UpdatedById, FonbecClaim.UserId },
        };

        var dialog = await DialogService.ShowAsync<SponsorshipEditDialog>(
            "Editar apadrinamiento",
            parameters);
        var result = await dialog.Result;
        if (result is not null && !result.Canceled)
        {
            await LoadAsync();
        }
    }

    private static Color StatusChipColor(SponsorshipTimelineStatus status) => status switch
    {
        SponsorshipTimelineStatus.Active => Color.Success,
        SponsorshipTimelineStatus.NotStarted => Color.Info,
        SponsorshipTimelineStatus.Finished => Color.Default,
        _ => Color.Default,
    };
}
