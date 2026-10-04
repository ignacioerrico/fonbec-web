using Fonbec.Web.DataAccess.DataModels.Sponsors.Input;
using Fonbec.Web.Logic.ExtensionMethods;
using Mapster;

namespace Fonbec.Web.Logic.Models.Sponsors.Input;

public record UpdateSponsorSendAlsoTosInputModel(
    int SponsorId,
    int? ChapterId,
    List<UpdateSendAlsoToInputModel> Recipients,
    int UpdatedById
);

public record UpdateSendAlsoToInputModel(
    int Id,
    string RecipientName,
    string RecipientEmail,
    bool SendAsBcc
);

public class UpdateSponsorSendAlsoTosInputModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<UpdateSponsorSendAlsoTosInputModel, UpdateSponsorSendAlsoTosInputDataModel>()
            .Map(dest => dest.SponsorId, src => src.SponsorId)
            .Map(dest => dest.ChapterId, src => src.ChapterId)
            .Map(dest => dest.Recipients, src => src.Recipients)
            .Map(dest => dest.UpdatedById, src => src.UpdatedById);

        config.NewConfig<UpdateSendAlsoToInputModel, UpdateSendAlsoToInputDataModel>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.RecipientName, src => src.RecipientName.MustBeNonEmpty().NormalizeText())
            .Map(dest => dest.RecipientEmail, src => src.RecipientEmail.MustBeNonEmpty().Trim().ToLower())
            .Map(dest => dest.SendAsBcc, src => src.SendAsBcc);
    }
}