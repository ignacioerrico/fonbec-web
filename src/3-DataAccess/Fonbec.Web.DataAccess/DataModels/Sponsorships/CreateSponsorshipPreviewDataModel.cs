using Fonbec.Web.DataAccess.DataModels.Sponsorships.Input;

namespace Fonbec.Web.DataAccess.DataModels.Sponsorships;

public class CreateSponsorshipPreviewDataModel
{
    public SponsorshipPeriodMatch PeriodMatch { get; init; }

    public IReadOnlyList<OverlappingSponsorshipToEndDataModel> OverlappingToEnd { get; init; } = [];
}