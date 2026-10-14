using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models.Chapters;
using Fonbec.Web.Logic.Models.Chapters.Input;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Components.NonPages.Dialogs;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Chapters;

[PageMetadata(nameof(ChaptersList), "Lista de filiales", [FonbecRole.Admin])]
public partial class ChaptersList : AuthenticationRequiredComponentBase
{
    private List<ChaptersListViewModel> _viewModels = [];

    private ChaptersListViewModel _originalViewModel = new();

    private string _searchString = string.Empty;

    [Inject]
    public IChapterService ChapterService { get; set; } = null!;

    [Inject]
    public IDialogService DialogService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        Loading = true;

        _viewModels = await ChapterService.GetChaptersForAdminListAsync();

        Loading = false;
    }

    private static IReadOnlyList<StaffGroup> StaffGroups(ChaptersListViewModel chapter) =>
    [
        new(FonbecRole.Manager, "Coordinadores", "Sin coordinadores.", chapter.Managers),
        new(FonbecRole.Uploader, "Mediadores", "Sin mediadores.", chapter.Facilitators),
        new(FonbecRole.Reviewer, "Revisores", "Sin revisores.", chapter.Reviewers),
    ];

    private async Task AddUserAsync(ChaptersListViewModel chapter, string role)
    {
        var parameters = new DialogParameters<ChapterUserCreateDialog>
        {
            { dialog => dialog.ChapterId, chapter.ChapterId },
            { dialog => dialog.Role, role },
            { dialog => dialog.CreatedById, FonbecClaim.UserId },
        };

        var dialog = await DialogService.ShowAsync<ChapterUserCreateDialog>(AddTitle(role), parameters);
        var result = await dialog.Result;
        if (result is null || result.Canceled)
        {
            return;
        }

        await ReloadChapterAsync(chapter.ChapterId);
    }

    private async Task ReloadChapterAsync(int chapterId)
    {
        Loading = true;

        var refreshed = await ChapterService.GetChaptersForAdminListAsync();
        var updated = refreshed.SingleOrDefault(chapter => chapter.ChapterId == chapterId);
        var current = _viewModels.SingleOrDefault(chapter => chapter.ChapterId == chapterId);

        Loading = false;

        if (updated is null || current is null)
        {
            _viewModels = refreshed;
            return;
        }

        current.Managers = updated.Managers;
        current.Facilitators = updated.Facilitators;
        current.Reviewers = updated.Reviewers;
        current.StudentCount = updated.StudentCount;
        current.SponsorCount = updated.SponsorCount;
    }

    private static string AddTitle(string role) => role switch
    {
        FonbecRole.Manager => "Agregar coordinador",
        FonbecRole.Uploader => "Agregar mediador",
        FonbecRole.Reviewer => "Agregar revisor",
        _ => "Agregar usuario",
    };

    private sealed record StaffGroup(
        string Role,
        string Title,
        string EmptyMessage,
        IReadOnlyList<ChapterStaffMemberViewModel> People);

    /// <summary>
    /// Called by MudTable to determine if a row should be displayed
    /// </summary>
    /// <param name="viewModel">The chapter view model to evaluate against the search string. Cannot be null.</param>
    /// <returns>true if the chapter matches the search string in any of the relevant fields; otherwise, false.</returns>
    private bool Filter(ChaptersListViewModel viewModel) =>
        string.IsNullOrWhiteSpace(_searchString)
        || viewModel.ChapterName.ContainsIgnoringAccents(_searchString);

    private void StartedEditingItem(ChaptersListViewModel originalViewModel) =>
        _originalViewModel = originalViewModel.DeepClone();

    private async Task CommittedItemChangesAsync(ChaptersListViewModel modifiedViewModel)
    {
        if (_originalViewModel.IsEqualTo(modifiedViewModel))
        {
            Snackbar.Add("No se realizaron cambios.", Severity.Info);
            return;
        }

        var updateChapterInputModel = new UpdateChapterInputModel(
            modifiedViewModel.ChapterId,
            modifiedViewModel.ChapterName,
            modifiedViewModel.Notes,
            FonbecClaim.UserId
        );

        Loading = true;

        var result = await ChapterService.UpdateChapterAsync(updateChapterInputModel);
        
        Loading = false;
        
        if (!result.AnyAffectedRows)
        {
            Snackbar.Add("No se pudo actualizar la filial.", Severity.Error);
            RevertItemChanges(modifiedViewModel.ChapterId);
            return;
        }

        _viewModels.Single(vm => vm.ChapterId == modifiedViewModel.ChapterId).LastUpdatedOnUtc = DateTime.Now;
    }

    private void RevertItemChanges(int chapterId)
    {
        var index = _viewModels.FindIndex(vm => vm.ChapterId == chapterId);
        if (index >= 0)
        {
            _viewModels[index] = _originalViewModel.DeepClone();
        }
    }
}
