using System;
using System.Collections.Generic;
using System.Text;
using Fonbec.Web.DataAccess.DataModels.Companies.Input;
using Fonbec.Web.Logic.ExtensionMethods;
using Mapster;

namespace Fonbec.Web.Logic.Models.Companies.Input;

public record UpdateCompanyInputModel(
    int CompanyId,
    string CompanyUpdatedName,
    string CompanyUpdatedPhoneNumber,
    string CompanyUpdatedEmail,
    string CompanyUpdatedNotes,
    int UpdatedById
);

public class UpdateCompanyInputModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<UpdateCompanyInputModel, UpdateCompanyInputDataModel>()
            .Map(dest => dest.CompanyId, src => src.CompanyId)
            .Map(dest => dest.CompanyUpdatedName, src => src.CompanyUpdatedName.MustBeNonEmpty().NormalizeText())
            .Map(dest => dest.CompanyUpdatedPhoneNumber, src => src.CompanyUpdatedPhoneNumber.NullOrTrimmed())
            .Map(dest => dest.CompanyUpdatedEmail, src => src.CompanyUpdatedEmail.NullOrTrimmed())
            .Map(dest => dest.CompanyUpdatedNotes, src => src.CompanyUpdatedNotes.NullOrTrimmed())
            .Map(dest => dest.UpdatedById, src => src.UpdatedById);
    }
}
