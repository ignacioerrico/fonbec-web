using Fonbec.Web.DataAccess.DataModels.Sponsorships.Input;
using Fonbec.Web.Logic.ExtensionMethods;
using Mapster;

namespace Fonbec.Web.Logic.Models.Sponsorships.Input;

public record CreateSponsorshipInputModel
(
    int StudentId,
    int SponsorId,
    DateTime SponsorshipStartDate,
    DateTime? SponsorshipEndDate,
    string SponsorshipNotes,
    int CreatedById
);

public class CreateSponsorshipInputModelMappsingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CreateSponsorshipInputModel, CreateSponsorshipInputDataModel>()
            .Map(dest => dest.StudentId, src => src.StudentId)
            .Map(dest => dest.SponsorId, src => src.SponsorId)
            .Map(dest => dest.SponsorshipStartDate, src => src.SponsorshipStartDate)
            .Map(dest => dest.SponsorshipEndDate, src => src.SponsorshipEndDate)
            .Map(dest => dest.SponsorshipNotes, src => src.SponsorshipNotes.NullOrTrimmed())
            .Map(dest => dest.CreatedById, src => src.CreatedById);
    }
}