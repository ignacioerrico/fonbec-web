using Fonbec.Web.DataAccess.DataModels.Sponsorship.Input;
using Fonbec.Web.Logic.ExtensionMethods;
using Mapster;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fonbec.Web.Logic.Models.Sponsorship.Input;

public record CreateSponsorshipInputModel(
    int StudentId,
    int SponsorId,
    DateTime? SponsorshipStartDate,
    DateTime? SponsorshipEndDate,
    string SponsorshipNotes,
    int CreatedById
    );

public class CreateSponsorshipInputModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateSponsorshipInputModel, CreateSponsorshipInputDataModel>()
            .Map(dest => dest.StudentId, src => src.StudentId)
            .Map(dest => dest.SponsorId, src => src.SponsorId)
            .Map(dest => dest.SponsorshipStartDate, src => src.SponsorshipStartDate)
            .Map(dest => dest.SponsorshipEndDate, src => src.SponsorshipEndDate)
            .Map(dest => dest.CreatedById, src => src.CreatedById)
            .Map(dest => dest.SponsorshipNotes, src => src.SponsorshipNotes.NullOrTrimmed());
    }
}