using Fonbec.Web.DataAccess.DataModels.Companies.Input;
using Fonbec.Web.Logic.ExtensionMethods;
using Mapster;

namespace Fonbec.Web.Logic.Models.Companies.Input;

public record UpdateCompanyRelationsInputModel(
    int CompanyId,
    List<UpdateCompanyContactInputModel> Contacts,
    List<SelectableModel<int>> Sponsors,
    int UpdatedById);

public record UpdateCompanyContactInputModel(
    int? Id,
    string FirstName,
    string LastName,
    string NickName,
    string Email,
    string PhoneNumber,
    string Notes);

public record UpdateCompanyRelationsResult(
    bool CompanyFound,
    int AffectedRows = 0,
    IReadOnlyList<MissingSponsor>? MissingSponsors = null,
    bool HasUnknownContacts = false)
{
    public bool AnyAffectedRows => AffectedRows > 0;

    public bool HasMissingSponsors => MissingSponsors is { Count: > 0 };
}

public class UpdateCompanyRelationsInputModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<UpdateCompanyRelationsInputModel, UpdateCompanyRelationsInputDataModel>()
            .Map(dest => dest.CompanyId, src => src.CompanyId)
            .Map(dest => dest.UpdatedById, src => src.UpdatedById)
            .Map(dest => dest.Contacts, src => src.Contacts)
            .Map(dest => dest.SponsorIds, src => src.Sponsors.Select(sponsor => sponsor.Key).Distinct());

        config.NewConfig<UpdateCompanyContactInputModel, UpdateCompanyContactInputDataModel>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.FirstName, src => src.FirstName.MustBeNonEmpty().NormalizeText())
            .Map(dest => dest.LastName, src => src.LastName.NormalizeText().NullOrTrimmed())
            .Map(dest => dest.NickName, src => src.NickName.NormalizeText().NullOrTrimmed())
            .Map(dest => dest.Email, src => src.Email.ToLower().NullOrTrimmed())
            .Map(dest => dest.PhoneNumber, src => src.PhoneNumber.NullOrTrimmed())
            .Map(dest => dest.Notes, src => src.Notes.NullOrTrimmed());
    }
}