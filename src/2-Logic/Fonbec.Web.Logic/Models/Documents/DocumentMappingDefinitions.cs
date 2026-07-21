using Fonbec.Web.DataAccess.DataModels.Documents;
using Fonbec.Web.DataAccess.DataModels.Documents.Input;
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

        config.NewConfig<SharedDocumentDataModel, SharedDocumentViewModel>();

        config.NewConfig<ReviewProgressDataModel, ReviewProgressViewModel>();

        config.NewConfig<LetterPlanProgressDataModel, LetterPlanProgressViewModel>();

        config.NewConfig<DocumentDescriptionOptionDataModel, DocumentDescriptionOptionViewModel>();
    }

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