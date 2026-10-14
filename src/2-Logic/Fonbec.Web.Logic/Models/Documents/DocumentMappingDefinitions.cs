using Fonbec.Web.DataAccess.DataModels.Documents;
using Fonbec.Web.DataAccess.DataModels.Documents.Input;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models.Documents.Input;
using Mapster;

namespace Fonbec.Web.Logic.Models.Documents;

public class DocumentMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateBlobPathInputModel, CreateBlobPathInputDataModel>();

        // The single optional Blob on the create input becomes the ordered page list on the data model
        // (empty for Text/YouTube). The with-blob upload path builds the multi-page list directly.
        config.NewConfig<CreateLetterInputModel, CreateLetterInputDataModel>()
            .Map(dest => dest.UploadedById, src => src.User.UserId)
            .Map(dest => dest.Blobs, src => SingleBlobToList(src.Blob));

        config.NewConfig<CreateReportCardInputModel, CreateReportCardInputDataModel>()
            .Map(dest => dest.UploadedById, src => src.User.UserId)
            .Map(dest => dest.Blobs, src => SingleBlobToList(src.Blob));

        config.NewConfig<CreateOtherDocumentInputModel, CreateOtherDocumentInputDataModel>()
            .Map(dest => dest.UploadedById, src => src.User.UserId)
            .Map(dest => dest.Blobs, src => SingleBlobToList(src.Blob));

        config.NewConfig<SubmitDigitalImprovementInputModel, SubmitDigitalImprovementInputDataModel>();

        config.NewConfig<ApproveLetterInputModel, ApproveLetterInputDataModel>()
            .Map(dest => dest.ReviewerId, src => src.ReviewerId);

        config.NewConfig<RejectLetterInputModel, RejectLetterInputDataModel>()
            .Map(dest => dest.ReviewerId, src => src.ReviewerId);

        config.NewConfig<ApproveReportCardInputModel, ApproveReportCardInputDataModel>()
            .Map(dest => dest.ReviewerId, src => src.ReviewerId);

        config.NewConfig<RejectReportCardInputModel, RejectReportCardInputDataModel>()
            .Map(dest => dest.ReviewerId, src => src.ReviewerId);

        config.NewConfig<ApproveOtherDocumentInputModel, ApproveOtherDocumentInputDataModel>()
            .Map(dest => dest.ReviewerId, src => src.ReviewerId);

        config.NewConfig<RejectOtherDocumentInputModel, RejectOtherDocumentInputDataModel>()
            .Map(dest => dest.ReviewerId, src => src.ReviewerId);

        config.NewConfig<DocumentQueueItemDataModel, DocumentQueueItemViewModel>();

        config.NewConfig<SharedDocumentDataModel, SharedDocumentViewModel>()
            .Map(dest => dest.Title, src => BuildSharedDocumentTitle(src));

        config.NewConfig<ReviewWorkspacePageDataModel, ReviewWorkspacePageViewModel>();

        config.NewConfig<ReviewProgressDataModel, ReviewProgressViewModel>();

        config.NewConfig<LetterPlanProgressDataModel, LetterPlanProgressViewModel>();

        config.NewConfig<DocumentDescriptionOptionDataModel, DocumentDescriptionOptionViewModel>();

        config.NewConfig<RejectedReasonDataModel, RejectedReasonViewModel>();

        config.NewConfig<StudentDocumentHistoryDataModel, StudentDocumentHistoryViewModel>();

        config.NewConfig<StudentDocumentHistoryItemDataModel, StudentDocumentHistoryItemViewModel>()
            .Map(dest => dest.Date, src => DateOnly.FromDateTime(src.UploadedOn))
            .Map(dest => dest.CanDownload, src => src.FileKind == FileKind.Blob && src.PageCount > 0)
            .Map(dest => dest.Detail, src => BuildStudentDocumentDetail(src));
    }

    private static string BuildStudentDocumentDetail(StudentDocumentHistoryItemDataModel src) =>
        src.DocumentType switch
        {
            DocumentType.Letter => src.RecipientName ?? string.Empty,
            DocumentType.ReportCard => JoinReportCardDetail(src.ReportCardPeriod, src.Description),
            DocumentType.Other => src.Description ?? string.Empty,
            _ => src.Description ?? string.Empty,
        };

    private static string JoinReportCardDetail(DateOnly? period, string? description)
    {
        var periodText = period?.ToDateTime(TimeOnly.MinValue).ToSpanishMonthYear();
        if (string.IsNullOrWhiteSpace(periodText))
        {
            return description ?? string.Empty;
        }

        return string.IsNullOrWhiteSpace(description)
            ? periodText
            : $"{periodText} — {description}";
    }

    private static string BuildSharedDocumentTitle(SharedDocumentDataModel src) =>
        src.DocumentType switch
        {
            DocumentType.Letter => src.PlanStartsOn is { } planStartsOn
                ? $"Carta de {planStartsOn.ToSpanishMonthYear()}"
                : "Carta",
            DocumentType.ReportCard => src.ReportCardPeriod is { } period
                ? $"{period.ToDateTime(TimeOnly.MinValue).ToSpanishMonthYear()} — {src.Description}"
                : src.Description ?? string.Empty,
            DocumentType.Other => src.Description ?? string.Empty,
            _ => src.Description ?? string.Empty,
        };

    private static List<CreateBlobPathInputDataModel> SingleBlobToList(CreateBlobPathInputModel? blob) =>
        blob is null
            ? []
            : [new CreateBlobPathInputDataModel
            {
                StoragePath = blob.StoragePath,
                MimeType = blob.MimeType,
                FileSizeBytes = blob.FileSizeBytes,
                Sha256 = blob.Sha256,
            }];
}