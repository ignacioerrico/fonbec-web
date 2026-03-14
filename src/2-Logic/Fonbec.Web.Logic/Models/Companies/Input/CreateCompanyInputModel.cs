using System;
using System.Collections.Generic;
using System.Text;
using Fonbec.Web.DataAccess.DataModels.Companies.Input;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models.Sponsors;
using Mapster;

namespace Fonbec.Web.Logic.Models.Companies.Input;

public record CreateCompanyInputModel(
    string CompanyName,
    string CompanyEmail,
    string CompanyPhoneNumber,
    List<CreateCompanySponsorsListViewModel> CompanySponsors,
    int CreatedById
);

public class CreateCompanyInputModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateCompanyInputModel, CreateCompanyInputDataModel>()
            .Map(dest => dest.CompanyName, src => src.CompanyName.MustBeNonEmpty().NormalizeText())
            .Map(dest => dest.CompanyEmail, src => src.CompanyEmail.NullOrTrimmed())
            .Map(dest => dest.CompanyPhoneNumber, src => src.CompanyPhoneNumber.NullOrTrimmed())
            .Map(dest => dest.CompanySponsorsIds, src => src.CompanySponsors)
            .Map(dest => dest.CreatedById, src => src.CreatedById);

        config.NewConfig<CreateCompanySponsorsListViewModel, int>()
            .Map(dest => dest, src => src.SponsorId);
    }
}