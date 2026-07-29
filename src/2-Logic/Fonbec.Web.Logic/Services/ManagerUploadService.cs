using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models.Documents.Input;
using Fonbec.Web.Logic.Models.Facilitators.Input;
using Fonbec.Web.Logic.Models.Managers;
using Fonbec.Web.Logic.Models.Managers.Input;
using Fonbec.Web.Logic.Models.Results;
using Fonbec.Web.Logic.Util;

namespace Fonbec.Web.Logic.Services;

public interface IManagerUploadService
{
    /// <summary>
    /// Resolves the read-only upload context for the given manager and student.
    /// Returns <c>null</c> when the document type is invalid, the student is not found /
    /// inactive, the student is not in the manager's chapter, or the (letter) trigger
    /// parameters are missing/inconsistent.
    /// </summary>
    Task<ManagerUploadContextViewModel?> GetUploadContextAsync(
        int managerId, int managerChapterId, int studentId, string documentType,
        int? sponsorId, int? companyId, int? planId);

    /// <summary>
    /// Resolves the letter recipient options for the given student, used by the type-picker
    /// entry flow from the Students list. Returns <c>null</c> when the student is not found,
    /// inactive, or not in the manager's chapter.
    /// </summary>
    Task<ManagerLetterRecipientOptionsViewModel?> GetLetterRecipientOptionsAsync(
        int managerChapterId, int studentId);

    Task<CrudResult<long>> UploadLetterAsync(ManagerUploadLetterInputModel input, int uploadedById);
    Task<CrudResult<long>> UploadReportCardAsync(ManagerUploadReportCardInputModel input, int uploadedById);
    Task<CrudResult<long>> UploadOtherDocumentAsync(ManagerUploadOtherInputModel input, int uploadedById);
}

public class ManagerUploadService(
    IManagerUploadRepository managerUploadRepository,
    IDocumentService documentService,
    IDocumentRepository documentRepository,
    ILetterExemptionService letterExemptionService,
    TimeProvider timeProvider) : IManagerUploadService
{
    public async Task<ManagerUploadContextViewModel?> GetUploadContextAsync(
        int managerId, int managerChapterId, int studentId, string documentType,
        int? sponsorId, int? companyId, int? planId)
    {
        var type = UploadDocumentHelper.ParseDocumentType(documentType);
        if (type is null)
        {
            return null;
        }

        var context = await managerUploadRepository.GetUploadContextAsync(studentId, planId, sponsorId, companyId);
        if (context is not { IsActive: true } || context.ChapterId != managerChapterId)
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

            // A student exempt from letters for this plan cannot have one uploaded on their behalf; block direct URLs too.
            if (await letterExemptionService.IsExemptAsync(studentId, planId.Value))
            {
                return null;
            }

            resolvedPlanId = planId;
            planPeriodLabel = UploadDocumentHelper.FormatPeriod(context.PlanStartsOn.Value);
        }

        return new ManagerUploadContextViewModel
        {
            StudentId = context.StudentId,
            StudentFullName = $"{context.StudentFirstName} {context.StudentLastName}".Trim(),
            ChapterId = context.ChapterId,
            DocumentType = type.Value,
            EducationLevel = UploadDocumentHelper.ResolveEducationLevel(
                context.SecondarySchoolStartYear, context.UniversityStartYear, timeProvider.GetUtcNow().UtcDateTime),
            FacilitatorFullName = $"{context.FacilitatorFirstName} {context.FacilitatorLastName}".Trim(),
            PlanId = resolvedPlanId,
            PlanPeriodLabel = planPeriodLabel,
            SponsorId = resolvedSponsorId,
            CompanyId = resolvedCompanyId,
            RecipientName = recipientName,
        };
    }

    public async Task<ManagerLetterRecipientOptionsViewModel?> GetLetterRecipientOptionsAsync(
        int managerChapterId, int studentId)
    {
        var context = await managerUploadRepository.GetUploadContextAsync(studentId, null, null, null);
        if (context is not { IsActive: true } || context.ChapterId != managerChapterId)
        {
            return null;
        }

        var currentPlan = await managerUploadRepository.GetCurrentPlanForChapterAsync(managerChapterId);
        if (currentPlan is null)
        {
            return new ManagerLetterRecipientOptionsViewModel();
        }

        var isExempt = await letterExemptionService.IsExemptAsync(studentId, currentPlan.PlanId);

        var options = isExempt
            ? []
            : await BuildRecipientOptionsAsync(studentId, currentPlan.PlanId);

        return new ManagerLetterRecipientOptionsViewModel
        {
            PlanId = currentPlan.PlanId,
            PlanPeriodLabel = UploadDocumentHelper.FormatPeriod(currentPlan.StartsOn),
            IsExempt = isExempt,
            Options = options,
        };
    }

    private async Task<List<ManagerLetterRecipientOptionViewModel>> BuildRecipientOptionsAsync(int studentId, int planId)
    {
        var sponsorships = await managerUploadRepository.GetActiveSponsorshipsAsync(studentId);

        var options = new List<ManagerLetterRecipientOptionViewModel>();
        foreach (var sponsorship in sponsorships)
        {
            // Defensive: a recipient with no resolvable name is not a valid letter target.
            if (string.IsNullOrWhiteSpace(sponsorship.RecipientName))
            {
                continue;
            }

            // Surface an existing (non-rejected) letter now so the manager sees it here instead of
            // only being blocked after filling in the upload form.
            var alreadyHasLetter = sponsorship.SponsorId.HasValue
                ? await documentRepository.HasDuplicateLetterAsync(studentId, sponsorship.SponsorId.Value, planId)
                : sponsorship.CompanyId.HasValue
                  && await documentRepository.HasDuplicateCompanyLetterAsync(studentId, sponsorship.CompanyId.Value, planId);

            options.Add(new ManagerLetterRecipientOptionViewModel
            {
                SponsorId = sponsorship.SponsorId,
                CompanyId = sponsorship.CompanyId,
                RecipientName = sponsorship.RecipientName,
                AlreadyHasLetterForPlan = alreadyHasLetter,
            });
        }

        return options;
    }

    public async Task<CrudResult<long>> UploadLetterAsync(
        ManagerUploadLetterInputModel input, int uploadedById)
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

        var user = BuildUserContext(uploadedById, input.ManagerChapterId);

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

    public async Task<CrudResult<long>> UploadReportCardAsync(
        ManagerUploadReportCardInputModel input, int uploadedById)
    {
        var user = BuildUserContext(uploadedById, input.ManagerChapterId);

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

    public async Task<CrudResult<long>> UploadOtherDocumentAsync(
        ManagerUploadOtherInputModel input, int uploadedById)
    {
        var user = BuildUserContext(uploadedById, input.ManagerChapterId);

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

    private static CreateDocumentUserContext BuildUserContext(int uploadedById, int managerChapterId) =>
        new(uploadedById, FonbecRole.Manager, ChapterId: managerChapterId, FonbecAuthClaim: null);
}