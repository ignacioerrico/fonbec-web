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
    public List<string> CompanyPOCs { get; set; } = [];

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
            .MaxDepth(1)
            .Map(dest => dest.CompanyId, src => src.CompanyId)
            .Map(dest => dest.CompanyName, src => src.CompanyName)
            .Map(dest => dest.CompanyEmail, src => src.CompanyEmail)
            .Map(dest => dest.CompanyPhoneNumber, src => src.CompanyPhoneNumber)
            .Map(dest => dest.CompanyNotes, src => src.CompanyNotes)
            .Map(dest => dest.CompanyPOCs, src => src.CompanyPOCs.Select(poc => poc.FullName().ToList()))
            .Map(dest => dest.CompanySponsors, src => src.CompanySponsors.Select(sponsor => sponsor.FullName()).ToList());

        config.NewConfig<CompaniesListViewModel, SelectableModel<int>>()
            .Map(dest => dest.Key, src => src.CompanyId)
            .Map(dest => dest.DisplayName, src => src.CompanyName);
    }
}