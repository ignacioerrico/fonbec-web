using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.Models.LetterFollowUp;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Constants;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.LetterFollowUp;

[PageMetadata(nameof(LetterFollowUp), "Seguimiento académico", [FonbecRole.Manager])]
public partial class LetterFollowUp
{
    private LetterFollowUpViewModel _viewModel = new();

    [Inject]
    public ILetterFollowUpService LetterFollowUpService { get; set; } = null!;

    [Inject]
    public IDialogService DialogService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        if (FonbecClaim.ChapterId is null)
        {
            Snackbar.Add("No se pudo determinar la filial.", Severity.Error);
            NavigationManager.NavigateTo(NavRoutes.Home);
            return;
        }

        await ReloadAsync();
    }

    private async Task ResolveAsync(LetterFollowUpTaskViewModel task)
    {
        var confirmed = await DialogService.ShowMessageBox(
            "Resolver tarea",
            $"¿Confirmás que querés resolver la tarea de {task.StudentFullName}?",
            yesText: "Resolver",
            cancelText: "Cancelar");

        if (confirmed != true)
        {
            return;
        }

        Loading = true;
        var success = await LetterFollowUpService.MarkTaskResolvedAsync(
            task.AssessmentId,
            task.Kind,
            FonbecClaim.ChapterId!.Value,
            FonbecClaim.UserId);

        if (!success)
        {
            Loading = false;
            Snackbar.Add("No se pudo resolver la tarea.", Severity.Error);
            return;
        }

        Snackbar.Add("Tarea resuelta.", Severity.Success);
        await ReloadAsync();
    }

    private async Task ChangePriorityAsync(
        LetterFollowUpTaskViewModel task,
        RedFlagPriority priority)
    {
        if (task.Priority == priority)
        {
            return;
        }

        Loading = true;
        var success = await LetterFollowUpService.SetRedFlagPriorityAsync(
            task.AssessmentId,
            FonbecClaim.ChapterId!.Value,
            priority);

        if (!success)
        {
            Loading = false;
            Snackbar.Add("No se pudo cambiar la prioridad.", Severity.Error);
            await ReloadAsync();
            return;
        }

        Snackbar.Add("Prioridad actualizada.", Severity.Success);
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        Loading = true;
        _viewModel = await LetterFollowUpService.GetOpenTasksAsync(
            FonbecClaim.ChapterId!.Value);
        Loading = false;
    }
}