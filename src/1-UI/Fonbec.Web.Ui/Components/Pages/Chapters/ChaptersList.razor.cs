using Fonbec.Web.Logic.Models.Chapters;
using Fonbec.Web.Logic.Services;
using Microsoft.AspNetCore.Components;

namespace Fonbec.Web.Ui.Components.Pages.Chapters;

public partial class ChaptersList
{
    private List<AllChaptersViewModel> _chapters = [];

    [Inject]
    public IChaptersListService ChaptersListService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        _chapters = await ChaptersListService.GetAllChaptersAsync();
    }
}