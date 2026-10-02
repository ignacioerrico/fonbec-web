using Fonbec.Web.DataAccess.DataModels.Sponsors.Input;
using Fonbec.Web.Logic.ExtensionMethods;
using Mapster;

namespace Fonbec.Web.Logic.Models.Sponsors.Input;

public record CreateSendAlsoToInputModel(
    string RecipientName,
    string RecipientEmail,
    bool SendAsBcc
);

public class CreateSendAlsoToInputModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateSendAlsoToInputModel, CreateSendAlsoToInputDataModel>()
            .Map(dest => dest.RecipientName, src => src.RecipientName.MustBeNonEmpty().NormalizeText())
            .Map(dest => dest.RecipientEmail, src => src.RecipientEmail.MustBeNonEmpty().Trim().ToLower())
            .Map(dest => dest.SendAsBcc, src => src.SendAsBcc);
    }
}