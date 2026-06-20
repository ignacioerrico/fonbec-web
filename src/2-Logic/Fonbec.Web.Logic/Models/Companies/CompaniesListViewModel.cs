using Fonbec.Web.DataAccess.DataModels.Companies;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.Logic.ExtensionMethods;
using Mapster;

namespace Fonbec.Web.Logic.Models.Companies;

public class CompaniesListViewModel : AuditableViewModel, IDetectChanges<CompaniesListViewModel>
{
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyEmail { get; set; } = string.Empty;
    public string CompanyPhoneNumber { get; set; } = string.Empty;
    public string CompanyNotes { get; set; } = string.Empty;
    public List<string> CompanySponsors { get; set; } = [];
    public List<string> CompanyPointsOfContact { get; set; } = [];

    public bool IsIdenticalTo(CompaniesListViewModel other) =>
        CompanyName == other.CompanyName.NormalizeText()
        && CompanyEmail == other.CompanyEmail.NullOrTrimmed()
        && CompanyPhoneNumber == other.CompanyPhoneNumber.NullOrTrimmed()
        && CompanyNotes == other.CompanyNotes.NullOrTrimmed();
}

public class CompanyListViewModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<AllCompaniesDataModel, CompaniesListViewModel>()
            .Map(dest => dest.CompanyId, src => src.CompanyId)
            .Map(dest => dest.CompanyName, src => src.CompanyName)
            .Map(dest => dest.CompanyEmail, src => src.CompanyEmail ?? string.Empty)
            .Map(dest => dest.CompanyPhoneNumber, src => src.CompanyPhoneNumber ?? string.Empty)
            .Map(dest => dest.CompanyNotes, src => src.Notes ?? string.Empty)
            .Map(dest => dest.CompanyPointsOfContact, src => (src.CompanyPointsOfContact ?? Enumerable.Empty<PointOfContact>()).Select(PocDisplayName).ToList())
            .Map(dest => dest.CompanySponsors, src => (src.CompanySponsors ?? Enumerable.Empty<Sponsor>()).Select(sponsor => sponsor.FullName()).ToList());

        config.NewConfig<CompaniesListViewModel, SelectableModel<int>>()
            .Map(dest => dest.Key, src => src.CompanyId)
            .Map(dest => dest.DisplayName, src => src.CompanyName);
    }

    private static string PocDisplayName(PointOfContact poc) =>
        string.IsNullOrWhiteSpace(poc.LastName) ? poc.FirstName : $"{poc.FirstName} {poc.LastName}";
}
