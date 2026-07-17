using System.Globalization;
using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models.Documents.Input;
using Fonbec.Web.Logic.Models.Facilitators;
using Fonbec.Web.Logic.Models.Facilitators.Input;
using Fonbec.Web.Logic.Models.Results;
using Fonbec.Web.Logic.Util;

namespace Fonbec.Web.Logic.Services;

public interface IFacilitatorUploadService
{
    /// <summary>
    /// Resolves the read-only upload context for the given facilitator and student.
    /// Returns <c>null</c> when the document type is invalid, the student is not found /
    /// inactive, the student is not assigned to the facilitator, or the (letter) trigger
    /// parameters are missing/inconsistent.
    /// </summary>
    Task<FacilitatorUploadContextViewModel?> GetUploadContextAsync(
        int facilitatorId, int studentId, string documentType,
        int? sponsorId, int? companyId, int? planId);

    Task<CrudResult<long>> UploadLetterAsync(FacilitatorUploadLetterInputModel input, int uploadedById);
    Task<CrudResult<long>> UploadReportCardAsync(FacilitatorUploadReportCardInputModel input, int uploadedById);
    Task<CrudResult<long>> UploadOtherDocumentAsync(FacilitatorUploadOtherInputModel input, int uploadedById);
}

public class FacilitatorUploadService(
    IFacilitatorRepository facilitatorRepository,
    IDocumentService documentService,
    ILetterExemptionService letterExemptionService) : IFacilitatorUploadService
{
    public const string TipoCarta = "carta";
    public const string TipoBoletin = "boletin";
    public const string TipoOtro = "otro";

    private static readonly CultureInfo EsAr = CultureInfo.GetCultureInfo("es-AR");

    public async Task<FacilitatorUploadContextViewModel?> GetUploadContextAsync(
        int facilitatorId, int studentId, string documentType,
        int? sponsorId, int? companyId, int? planId)
    {
        var type = ParseDocumentType(documentType);
        if (type is null)
        {
            return null;
        }

        var context = await facilitatorRepository.GetUploadContextAsync(studentId, planId, sponsorId, companyId);
        if (context is not { IsActive: true } || context.FacilitatorId != facilitatorId)
        {
            return null;
        }

        int? resolvedPlanId = null;
        int? resolvedSponsorId = null;
        int? resolvedCompanyId = null;
        string? recipientName = null;
        string? planPeriodLabel = null;

        if (type == DocumentType.Letter)
        {
            // Letters require an active plan and exactly one recipient (sponsor XOR company).
            if (!planId.HasValue || context.PlanStartsOn is null)
            {
                return null;
            }

            if (sponsorId.HasValue == companyId.HasValue)
            {
                return null;
            }

            if (sponsorId.HasValue)
            {
                if (context.SponsorFirstName is null)
                {
                    return null;
                }

                recipientName = $"{context.SponsorFirstName} {context.SponsorLastName}".Trim();
                resolvedSponsorId = sponsorId;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(context.CompanyName))
                {
                    return null;
                }

                recipientName = context.CompanyName;
                resolvedCompanyId = companyId;
            }

            // A student exempt from letters for this plan cannot upload one (us110); block direct URLs too.
            if (await letterExemptionService.IsExemptAsync(studentId, planId.Value))
            {
                return null;
            }

            resolvedPlanId = planId;
            planPeriodLabel = FormatPeriod(context.PlanStartsOn.Value);
        }

        return new FacilitatorUploadContextViewModel
        {
            StudentId = context.StudentId,
            StudentFullName = $"{context.StudentFirstName} {context.StudentLastName}".Trim(),
            ChapterId = context.ChapterId,
            DocumentType = type.Value,
            EducationLevel = ResolveEducationLevel(context.SecondarySchoolStartYear, context.UniversityStartYear),
            PlanId = resolvedPlanId,
            PlanPeriodLabel = planPeriodLabel,
            SponsorId = resolvedSponsorId,
            CompanyId = resolvedCompanyId,
            RecipientName = recipientName,
        };
    }

    public async Task<CrudResult<long>> UploadLetterAsync(FacilitatorUploadLetterInputModel input, int uploadedById)
    {
        // A letter is addressed to exactly one recipient: a sponsor XOR a company.
        if (input.SponsorId.HasValue == input.CompanyId.HasValue)
        {
            return new CrudResult<long>(Errors: [DocumentMessages.LetterRequiresRecipient]);
        }

        if (await letterExemptionService.IsExemptAsync(input.StudentId, input.PlanId))
        {
            return new CrudResult<long>(Errors: [DocumentMessages.LetterExemptForPlan]);
        }

        var user = BuildUserContext(uploadedById);

        switch (input.ContentMode)
        {
            case UploadContentMode.File:
                if (input.Files is null || input.Files.Count == 0)
                {
                    return new CrudResult<long>(Errors: [DocumentMessages.BlobContentRequired]);
                }

                return await documentService.CreateLetterWithBlobAsync(new CreateLetterWithBlobInputModel(
                    input.StudentId, input.PlanId, input.SponsorId, user,
                    input.Files, input.UploaderNotes, input.CompanyId));

            case UploadContentMode.Text:
                return await documentService.CreateLetterAsync(new CreateLetterInputModel(
                    input.StudentId, input.PlanId, input.SponsorId, user,
                    FileKind.Text, TextContent: input.TextContent, UploaderNotes: input.UploaderNotes,
                    CompanyId: input.CompanyId));

            case UploadContentMode.YouTube:
                if (!YouTubeVideoIdParser.TryParse(input.YouTubeUrlOrId, out var videoId))
                {
                    return new CrudResult<long>(Errors: [DocumentMessages.YouTubeVideoIdRequired]);
                }

                return await documentService.CreateLetterAsync(new CreateLetterInputModel(
                    input.StudentId, input.PlanId, input.SponsorId, user,
                    FileKind.YouTube, YouTubeVideoId: videoId, UploaderNotes: input.UploaderNotes,
                    CompanyId: input.CompanyId));

            default:
                return new CrudResult<long>(Errors: [DocumentMessages.BlobContentRequired]);
        }
    }

    public async Task<CrudResult<long>> UploadReportCardAsync(FacilitatorUploadReportCardInputModel input, int uploadedById)
    {
        var user = BuildUserContext(uploadedById);

        switch (input.ContentMode)
        {
            case UploadContentMode.File:
                if (input.Files is null || input.Files.Count == 0)
                {
                    return new CrudResult<long>(Errors: [DocumentMessages.BlobContentRequired]);
                }

                return await documentService.CreateReportCardWithBlobAsync(new CreateReportCardWithBlobInputModel(
                    input.StudentId, user, input.Files,
                    input.Period, input.Description, input.UploaderNotes));

            case UploadContentMode.YouTube:
                if (!YouTubeVideoIdParser.TryParse(input.YouTubeUrlOrId, out var videoId))
                {
                    return new CrudResult<long>(Errors: [DocumentMessages.YouTubeVideoIdRequired]);
                }

                return await documentService.CreateReportCardAsync(new CreateReportCardInputModel(
                    input.StudentId, user, FileKind.YouTube, input.Period, input.Description,
                    YouTubeVideoId: videoId, UploaderNotes: input.UploaderNotes));

            case UploadContentMode.Text:
                return new CrudResult<long>(Errors: [DocumentMessages.ReportCardCannotUseText]);

            default:
                return new CrudResult<long>(Errors: [DocumentMessages.BlobContentRequired]);
        }
    }

    public async Task<CrudResult<long>> UploadOtherDocumentAsync(FacilitatorUploadOtherInputModel input, int uploadedById)
    {
        var user = BuildUserContext(uploadedById);

        switch (input.ContentMode)
        {
            case UploadContentMode.File:
                if (input.Files is null || input.Files.Count == 0)
                {
                    return new CrudResult<long>(Errors: [DocumentMessages.BlobContentRequired]);
                }

                return await documentService.CreateOtherDocumentWithBlobAsync(new CreateOtherDocumentWithBlobInputModel(
                    input.StudentId, user, input.Files,
                    input.Description, input.UploaderNotes));

            case UploadContentMode.Text:
                return await documentService.CreateOtherDocumentAsync(new CreateOtherDocumentInputModel(
                    input.StudentId, user, FileKind.Text, input.Description,
                    TextContent: input.TextContent, UploaderNotes: input.UploaderNotes));

            case UploadContentMode.YouTube:
                if (!YouTubeVideoIdParser.TryParse(input.YouTubeUrlOrId, out var videoId))
                {
                    return new CrudResult<long>(Errors: [DocumentMessages.YouTubeVideoIdRequired]);
                }

                return await documentService.CreateOtherDocumentAsync(new CreateOtherDocumentInputModel(
                    input.StudentId, user, FileKind.YouTube, input.Description,
                    YouTubeVideoId: videoId, UploaderNotes: input.UploaderNotes));

            default:
                return new CrudResult<long>(Errors: [DocumentMessages.BlobContentRequired]);
        }
    }

    private static CreateDocumentUserContext BuildUserContext(int uploadedById) =>
        new(uploadedById, FonbecRole.Uploader, ChapterId: null, FonbecAuthClaim: null);

    private static DocumentType? ParseDocumentType(string? documentType) =>
        documentType?.Trim().ToLowerInvariant() switch
        {
            TipoCarta => DocumentType.Letter,
            TipoBoletin => DocumentType.ReportCard,
            TipoOtro => DocumentType.Other,
            _ => null,
        };

    private static string FormatPeriod(DateTime startsOn)
    {
        var label = startsOn.ToString("MMM yyyy", EsAr).Replace(".", string.Empty);
        return EsAr.TextInfo.ToTitleCase(label);
    }

    private static EducationLevel ResolveEducationLevel(DateTime? secondarySchoolStartYear, DateTime? universityStartYear)
    {
        var now = DateTime.UtcNow;
        if (universityStartYear <= now)
        {
            return EducationLevel.University;
        }

        return secondarySchoolStartYear <= now
            ? EducationLevel.SecondarySchool
            : EducationLevel.PrimarySchool;
    }
}