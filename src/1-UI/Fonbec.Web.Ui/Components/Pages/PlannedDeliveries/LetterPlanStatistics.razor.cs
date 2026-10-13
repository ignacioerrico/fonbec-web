using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models.LetterPlanStatistics;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Constants;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.PlannedDeliveries;

[PageMetadata(nameof(LetterPlanStatistics), "Estadísticas de una campaña completada", [FonbecRole.Manager])]
public partial class LetterPlanStatistics
{
    private LetterPlanStatisticsViewModel? _viewModel;
    private bool _accessDenied;
    private string _pageTitle = "Estadísticas de la campaña";

    [Parameter]
    public int PlanId { get; set; }

    [Inject]
    public ILetterPlanStatisticsService LetterPlanStatisticsService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        if (FonbecClaim.ChapterId is null)
        {
            _accessDenied = true;
            return;
        }

        Loading = true;
        _viewModel = await LetterPlanStatisticsService.GetStatisticsAsync(PlanId, FonbecClaim.ChapterId.Value);
        Loading = false;

        if (_viewModel is null)
        {
            _accessDenied = true;
            Snackbar.Add("No tenés acceso a esta planificación.", Severity.Error);
            NavigationManager.NavigateTo(NavRoutes.PlannedDeliveries);
            return;
        }

        _pageTitle = $"Estadísticas de {_viewModel.PlanLabel.CapitalizeFirstLetter()}";
    }

    private static List<ChartSeries> SingleSeries(double[] counts) =>
    [
        new() { Name = "Cartas", Data = counts },
    ];

    private static List<ChartSeries> ActivitySeries(StackedCountChartViewModel activity) =>
    [
        new() { Name = "Aprobaciones", Data = activity.Approvals },
        new() { Name = "Rechazos", Data = activity.Rejections },
    ];

    private static List<ChartSeries> ScoreSeries(GroupedCountChartViewModel scores) =>
    [
        new() { Name = "Ortografía", Data = scores.Spelling },
        new() { Name = "Legibilidad", Data = scores.Penmanship },
        new() { Name = "Contenido", Data = scores.Content },
    ];
}