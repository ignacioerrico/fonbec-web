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

        config.NewConfig<CreateLetterInputModel, CreateLetterInputDataModel>()
            .Map(dest => dest.UploadedById, src => src.User.UserId);

        config.NewConfig<CreateReportCardInputModel, CreateReportCardInputDataModel>()
            .Map(dest => dest.UploadedById, src => src.User.UserId);

        config.NewConfig<CreateOtherDocumentInputModel, CreateOtherDocumentInputDataModel>()
            .Map(dest => dest.UploadedById, src => src.User.UserId);

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
}