using Fonbec.Web.DataAccess.DataModels.Companies;
using Fonbec.Web.Logic.ExtensionMethods;
using Mapster;

namespace Fonbec.Web.Logic.Models.Companies;

public class CompaniesListViewModel : AuditableViewModel, IDetectChanges<CompaniesListViewModel>
{
    public int CompanyId { get; set; }
    public string CompanyName { get; init; } = string.Empty;
    public string CompanyEmail { get; init; } = string.Empty;
    public string CompanyPhoneNumber { get; init; } = string.Empty;

    public bool IsIdenticalTo(CompaniesListViewModel other) =>
        CompanyName == other.CompanyName.NormalizeText()
        && CompanyEmail == other.CompanyEmail.NullOrTrimmed()
        && CompanyPhoneNumber == other.CompanyPhoneNumber.NullOrTrimmed();
}

public class CompanyListViewModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<AllCompaniesDataModel, CompaniesListViewModel>()
            .Map(dest => dest.CompanyId, src => src.CompanyId)
            .Map(dest => dest.CompanyName, src => src.CompanyName)
            .Map(dest => dest.CompanyEmail, src => src.CompanyEmail)
            .Map(dest => dest.CompanyPhoneNumber, src => src.CompanyPhoneNumber);

        config.NewConfig<CompaniesListViewModel, SelectableModel<int>>()
            .Map(dest => dest.Key, src => src.CompanyId)
            .Map(dest => dest.DisplayName, src => src.CompanyName);
    }
}