using Fonbec.Web.Logic.Models;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Models.Student;
using Microsoft.AspNetCore.Components;
using Fonbec.Web.Ui.Components.Pages.Sponsors;
namespace Fonbec.Web.Ui.Components.NonPages.Selectors;

public partial class SponsorSelector
{
    private bool _dataLoaded;

    private readonly List<SelectableModel<int>> _sponsors = [];

    [Parameter]
    public int SelectedSponsorId { get; set; }

    [Parameter]
    public EventCallback<int> SelectedSponsorIdChanged { get; set; }

    /// <summary>
    /// Callback invoked when sponsors are loaded. The int parameter indicates the number of chapters loaded.
    /// </summary>
    [Parameter]
    public EventCallback<int> OnSponsorsLoaded { get; set; }

    [Inject]
    public ISponsorService SponsorService { get; set; } = null!;

    // added (con shared service)
    [Inject]
    public StudentStateServicePayloadModel StudentChapterId { get; set; }    
    protected override async Task OnInitializedAsync() 
    {
        var sponsors = await SponsorService.GetAllSponsorsForSelectionAsync(StudentChapterId.ChapterId); 
        
        _dataLoaded = true; 
        
        _sponsors.AddRange(sponsors); 
        
        await OnSponsorsLoaded.InvokeAsync(sponsors.Count); 
        
        if (sponsors.Count > 0) 
        {
            SelectedSponsorId = sponsors.First().Key; 
            await OnSelectedValueChanged(SelectedSponsorId); 
        } 

        await base.OnInitializedAsync(); }
   
    private async Task OnSelectedValueChanged(int selectedSponsorId)
    {
        await SelectedSponsorIdChanged.InvokeAsync(selectedSponsorId);
    }
    
}