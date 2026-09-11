using Fonbec.Web.DataAccess.DataModels.Sponsorships.Input;
using Fonbec.Web.Logic.ExtensionMethods;
using Mapster;

namespace Fonbec.Web.Logic.Models.Sponsorships.Input;

public record UpdateSponsorshipInputModel(
    int SponsorshipId,
    DateTime SponsorshipStartDate,
    DateTime? SponsorshipEndDate,
    string SponsorshipNotes,
    int UpdatedById,
    bool ConfirmExemptionRevocation = false);

public class UpdateSponsorshipInputModelMappingDefinitions : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<UpdateSponsorshipInputModel, UpdateSponsorshipInputDataModel>()
            .Map(dest => dest.SponsorshipId, src => src.SponsorshipId)
            .Map(dest => dest.SponsorshipStartDate, src => src.SponsorshipStartDate)
            .Map(dest => dest.SponsorshipEndDate, src => src.SponsorshipEndDate)
            .Map(dest => dest.SponsorshipNotes, src => src.SponsorshipNotes.NullOrTrimmed())
            .Map(dest => dest.UpdatedById, src => src.UpdatedById)
            .Map(dest => dest.ConfirmExemptionRevocation, src => src.ConfirmExemptionRevocation);
    }
}
