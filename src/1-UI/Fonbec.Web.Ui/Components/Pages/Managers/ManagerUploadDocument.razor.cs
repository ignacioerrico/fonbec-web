using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.Models.Managers;
using Fonbec.Web.Logic.Models.Managers.Input;
using Fonbec.Web.Logic.Models.Results;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Constants;
using Fonbec.Web.Ui.Models.Documents;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Managers;

[PageMetadata(nameof(ManagerUploadDocument), "Subir documento", [FonbecRole.Manager])]
public partial class ManagerUploadDocument : AuthenticationRequiredComponentBase
{
    private ManagerUploadContextViewModel? _context;
    private int _managerChapterId;

    [Parameter]
    public int StudentId { get; set; }

    [SupplyParameterFromQuery(Name = "tipo")]
    public string? Tipo { get; set; }

    [SupplyParameterFromQuery(Name = "padrinoId")]
    public int? PadrinoId { get; set; }

    [SupplyParameterFromQuery(Name = "empresaId")]
    public int? EmpresaId { get; set; }

    [SupplyParameterFromQuery(Name = "planId")]
    public int? PlanId { get; set; }

    [Inject]
    public IManagerUploadService ManagerUploadService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        Loading = true;

        // Managers with a null chapter claim should not occur, but cannot upload if it happens.
        if (FonbecClaim.ChapterId is not { } managerChapterId)
        {
            Snackbar.Add("No se puede subir el documento para este becario.", Severity.Error);
            NavigationManager.NavigateTo(NavRoutes.Students);
            return;
        }

        _managerChapterId = managerChapterId;

        _context = await ManagerUploadService.GetUploadContextAsync(
            FonbecClaim.UserId, managerChapterId, StudentId, Tipo ?? string.Empty, PadrinoId, EmpresaId, PlanId);

        if (_context is null)
        {
            Snackbar.Add("No se puede subir el documento para este becario.", Severity.Error);
            NavigationManager.NavigateTo(NavRoutes.Students);
            return;
        }

        Loading = false;
    }

    private async Task<CrudResult<long>> HandleSubmitAsync(UploadDocumentFormSubmission submission) =>
        _context!.DocumentType switch
        {
            DocumentType.Letter => await ManagerUploadService.UploadLetterAsync(
                new ManagerUploadLetterInputModel(
                    _context.StudentId, _managerChapterId, _context.PlanId!.Value, _context.SponsorId, _context.CompanyId,
                    submission.ContentMode, submission.Files, submission.TextContent,
                    submission.YouTubeUrlOrId, submission.UploaderNotes),
                FonbecClaim.UserId),

            DocumentType.ReportCard => await ManagerUploadService.UploadReportCardAsync(
                new ManagerUploadReportCardInputModel(
                    _context.StudentId, _managerChapterId, submission.Period!.Value, submission.Description,
                    submission.ContentMode, submission.Files, submission.YouTubeUrlOrId, submission.UploaderNotes),
                FonbecClaim.UserId),

            _ => await ManagerUploadService.UploadOtherDocumentAsync(
                new ManagerUploadOtherInputModel(
                    _context.StudentId, _managerChapterId, submission.Description, submission.ContentMode,
                    submission.Files, submission.TextContent, submission.YouTubeUrlOrId, submission.UploaderNotes),
                FonbecClaim.UserId),
        };
}