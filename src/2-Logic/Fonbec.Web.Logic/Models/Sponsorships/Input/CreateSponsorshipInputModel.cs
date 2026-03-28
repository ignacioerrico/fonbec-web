using Fonbec.Web.DataAccess.DataModels.Sponsorships.Input;
using Fonbec.Web.Logic.ExtensionMethods;
using Mapster;

namespace Fonbec.Web.Logic.Models.Sponsorships.Input;

public record CreateSponsorshipInputModel(
    int StudentId,
    SelectableModel<int>? Sponsor,
    int? CompanyId,
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
            .BeforeMapping((src, dest) =>
            {
                if ((src.Sponsor is null) == (src.CompanyId is null))
                {
                    throw new InvalidOperationException("Business rule violation: only one of SponsorId or CompanyId must be null");
                }
            })
            .Map(dest => dest.StudentId, src => src.StudentId)
            .Map(dest => dest.SponsorId, src => src.Sponsor!.Key, srcCond => srcCond.Sponsor != null)
            .Map(dest => dest.SponsorId, src => (int?)null, srcCond => srcCond.Sponsor == null)
            .Map(dest => dest.CompanyId, src => src.CompanyId)
            .Map(dest => dest.SponsorshipStartDate, src => src.SponsorshipStartDate)
            .Map(dest => dest.SponsorshipEndDate, src => src.SponsorshipEndDate)
            .Map(dest => dest.SponsorshipNotes, src => src.SponsorshipNotes.NullOrTrimmed())
            .Map(dest => dest.CreatedById, src => src.CreatedById);
    }
}