using Fonbec.Web.DataAccess.DataModels.Companies.Input;
using Fonbec.Web.Logic.ExtensionMethods;
using Mapster;

namespace Fonbec.Web.Logic.Models.Companies.Input;

public record CreatePointOfContactInputModel(
    string FirstName,
    string LastName,
    string? NickName,
    string? Email,
    string? PhoneNumber,
    int CreatedById
);

public class CreatePointOfContactInputModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreatePointOfContactInputModel, CreatePointOfContactInputDataModel>()
            .Map(dest => dest.FirstName, src => src.FirstName.MustBeNonEmpty().NormalizeText())
            .Map(dest => dest.LastName, src => src.LastName.MustBeNonEmpty().NormalizeText())
            .Map(dest => dest.NickName, src => src.NickName.NullOrTrimmed())
            .Map(dest => dest.Email, src => src.Email.NullOrTrimmed())
            .Map(dest => dest.PhoneNumber, src => src.PhoneNumber.NullOrTrimmed())
            .Map(dest => dest.CreatedById, src => src.CreatedById);
    }
}
