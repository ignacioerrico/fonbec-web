using Fonbec.Web.DataAccess.DataModels.Companies.Input;
using Fonbec.Web.Logic.ExtensionMethods;
using Mapster;

namespace Fonbec.Web.Logic.Models.Companies.Input;

public record CreateCompanyInputModel(
    string CompanyName,
    string CompanyEmail,
    string CompanyPhoneNumber,
    string CompanyNotes,
    List<CreateCompanyPointOfContactInputModel> PointsOfContact,
    List<SelectableModel<int>> Sponsors,
    int CreatedById
);

public record CreateCompanyPointOfContactInputModel(
    string PocFirstName,
    string PocLastName,
    string PocNickName,
    string PocEmail,
    string PocPhoneNumber,
    string PocNotes
);

public class CreateCompanyInputModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateCompanyInputModel, CreateCompanyInputDataModel>()
            .Map(dest => dest.CompanyName, src => src.CompanyName.MustBeNonEmpty().NormalizeText())
            .Map(dest => dest.CompanyEmail, src => src.CompanyEmail.ToLower().NullOrTrimmed())
            .Map(dest => dest.CompanyPhoneNumber, src => src.CompanyPhoneNumber.NullOrTrimmed())
            .Map(dest => dest.CompanyNotes, src => src.CompanyNotes.NullOrTrimmed())
            .Map(dest => dest.PointsOfContact, src => src.PointsOfContact)
            .Map(dest => dest.SponsorIds, src => src.Sponsors.Select(sm => sm.Key))
            .Map(dest => dest.CreatedById, src => src.CreatedById);

        config.NewConfig<CreateCompanyPointOfContactInputModel, CreateCompanyPointOfContactInputDataModel>()
            .Map(dest => dest.PocFirstName, src => src.PocFirstName.MustBeNonEmpty().NormalizeText())
            .Map(dest => dest.PocLastName, src => src.PocLastName.NormalizeText().NullOrTrimmed())
            .Map(dest => dest.PocNickName, src => src.PocNickName.NormalizeText().NullOrTrimmed())
            .Map(dest => dest.PocEmail, src => src.PocEmail.ToLower().NullOrTrimmed())
            .Map(dest => dest.PocPhoneNumber, src => src.PocPhoneNumber.NullOrTrimmed())
            .Map(dest => dest.PocNotes, src => src.PocNotes.NullOrTrimmed());
    }
}
