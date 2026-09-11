namespace Fonbec.Web.DataAccess.DataModels.Sponsorships.Input;

public class UpdateSponsorshipInputDataModel
{
    public int SponsorshipId { get; set; }
    public DateTime SponsorshipStartDate { get; set; }
    public DateTime? SponsorshipEndDate { get; set; }
    public string? SponsorshipNotes { get; set; }
    public int UpdatedById { get; set; }
    public bool ConfirmExemptionRevocation { get; set; }
}
