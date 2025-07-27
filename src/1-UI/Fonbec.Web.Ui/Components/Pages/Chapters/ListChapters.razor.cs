using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Models;
using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.Pages.Chapters;

public partial class ListChapters
{
    private List<Chapter> _chapters = new();

    [Inject]
    public IListChaptersService ListChaptersService { get; set; } = null!;

    protected override void OnInitialized()
    {
        _chapters = ListChaptersService.GetAllChapters().ToList();
    }
}