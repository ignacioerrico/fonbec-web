using Fonbec.Web.DataAccess.DataModels.Companies;
using Mapster;

namespace Fonbec.Web.Logic.Models.Companies;

public class CompanyRelationsViewModel
{
    public int CompanyId { get; set; }

    public string CompanyName { get; set; } = string.Empty;

    public List<CompanyContactViewModel> Contacts { get; set; } = [];

    public List<SelectableModel<int>> Sponsors { get; set; } = [];
}

public class CompanyContactViewModel
{
    public int Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string NickName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;
}

public class CompanyRelationsViewModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CompanyRelationsDataModel, CompanyRelationsViewModel>()
            .Map(dest => dest.CompanyId, src => src.CompanyId)
            .Map(dest => dest.CompanyName, src => src.CompanyName)
            .Map(dest => dest.Contacts, src => src.Contacts)
            .Map(dest => dest.Sponsors, src => src.Sponsors);

        config.NewConfig<CompanyContactDataModel, CompanyContactViewModel>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.FirstName, src => src.FirstName)
            .Map(dest => dest.LastName, src => src.LastName ?? string.Empty)
            .Map(dest => dest.NickName, src => src.NickName ?? string.Empty)
            .Map(dest => dest.Email, src => src.Email ?? string.Empty)
            .Map(dest => dest.PhoneNumber, src => src.PhoneNumber ?? string.Empty)
            .Map(dest => dest.Notes, src => src.Notes ?? string.Empty);

        config.NewConfig<CompanyLinkedSponsorDataModel, SelectableModel<int>>()
            .Map(dest => dest.Key, src => src.Id)
            .Map(dest => dest.DisplayName, src => src.FullName);
    }
}