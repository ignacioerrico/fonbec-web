using Fonbec.Web.DataAccess.DataModels.Companies.Input;
using Fonbec.Web.Logic.ExtensionMethods;
using Mapster;

namespace Fonbec.Web.Logic.Models.Companies.Input;

public record CreateCompanyWithPointsOfContactInputModel(
    string CompanyName,
    string CompanyEmail,
    string CompanyPhoneNumber,
    int CreatedById,
    List<CreatePointOfContactInputModel> PointsOfContact
);

public class CreateCompanyWithPointsOfContactInputModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateCompanyWithPointsOfContactInputModel, CreateCompanyWithPointsOfContactInputDataModel>()
            .Map(dest => dest.CompanyName, src => src.CompanyName.MustBeNonEmpty().NormalizeText())
            .Map(dest => dest.CompanyEmail, src => src.CompanyEmail.NullOrTrimmed())
            .Map(dest => dest.CompanyPhoneNumber, src => src.CompanyPhoneNumber.NullOrTrimmed())
            .Map(dest => dest.CreatedById, src => src.CreatedById)
            .Map(dest => dest.PointsOfContact, src => src.PointsOfContact);
    }
}