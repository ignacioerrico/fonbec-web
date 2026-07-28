using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.Models.Facilitators;
using Fonbec.Web.Logic.Models.Facilitators.Input;
using Fonbec.Web.Logic.Models.Results;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Ui.Constants;
using Fonbec.Web.Ui.Models.Documents;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Fonbec.Web.Ui.Components.Pages.Facilitators;

[PageMetadata(nameof(FacilitatorUploadDocument), "Subir documento", [FonbecRole.Uploader])]
public partial class FacilitatorUploadDocument : AuthenticationRequiredComponentBase
{
    private FacilitatorUploadContextViewModel? _context;

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
    public IFacilitatorUploadService FacilitatorUploadService { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        Loading = true;

        _context = await FacilitatorUploadService.GetUploadContextAsync(
            FonbecClaim.UserId, StudentId, Tipo ?? string.Empty, PadrinoId, EmpresaId, PlanId);

        if (_context is null)
        {
            Snackbar.Add("No se puede subir el documento para este becario.", Severity.Error);
            NavigationManager.NavigateTo(NavRoutes.FacilitatorStudents);
            return;
        }

        Loading = false;
    }

    private async Task<CrudResult<long>> HandleSubmitAsync(UploadDocumentFormSubmission submission) =>
        _context!.DocumentType switch
        {
            DocumentType.Letter => await FacilitatorUploadService.UploadLetterAsync(
                new FacilitatorUploadLetterInputModel(
                    _context.StudentId, _context.PlanId!.Value, _context.SponsorId, _context.CompanyId,
                    submission.ContentMode, submission.Files, submission.TextContent,
                    submission.YouTubeUrlOrId, submission.UploaderNotes),
                FonbecClaim.UserId),

            DocumentType.ReportCard => await FacilitatorUploadService.UploadReportCardAsync(
                new FacilitatorUploadReportCardInputModel(
                    _context.StudentId, submission.Period!.Value, submission.Description,
                    submission.ContentMode, submission.Files, submission.YouTubeUrlOrId, submission.UploaderNotes),
                FonbecClaim.UserId),

            _ => await FacilitatorUploadService.UploadOtherDocumentAsync(
                new FacilitatorUploadOtherInputModel(
                    _context.StudentId, submission.Description, submission.ContentMode,
                    submission.Files, submission.TextContent, submission.YouTubeUrlOrId, submission.UploaderNotes),
                FonbecClaim.UserId),
        };
}