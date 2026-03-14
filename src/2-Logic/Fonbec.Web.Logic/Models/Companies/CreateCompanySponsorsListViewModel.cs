using System;
using System.Collections.Generic;
using System.Text;
using Fonbec.Web.DataAccess.DataModels.Companies;
using Fonbec.Web.DataAccess.DataModels.Sponsors;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models.Companies.Input;
using Mapster;

namespace Fonbec.Web.Logic.Models.Companies;

public class CreateCompanySponsorsListViewModel : AuditableViewModel, IDetectChanges<CreateCompanySponsorsListViewModel>
{
    public int SponsorId  {  get; set; }
    public string SponsorFullName { get; init; } = string.Empty;
    public bool IsIdenticalTo(CreateCompanySponsorsListViewModel other) =>
        SponsorId == other.SponsorId && SponsorFullName == other.SponsorFullName;
}

public class CreateCompanySponsorsListViewModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateCompanySponsorDataModel, CreateCompanySponsorsListViewModel>()
            .Map(dest => dest.SponsorId, src => src.SponsorId) 
            .Map(dest => dest.SponsorFullName, src => src.FullName());
    }
}

