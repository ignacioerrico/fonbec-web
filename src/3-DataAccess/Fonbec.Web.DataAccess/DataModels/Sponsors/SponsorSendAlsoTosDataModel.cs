namespace Fonbec.Web.DataAccess.DataModels.Sponsors;

public class SponsorSendAlsoTosDataModel
{
    public int SponsorId { get; set; }

    public string SponsorFirstName { get; set; } = null!;

    public string SponsorLastName { get; set; } = null!;

    public string SponsorEmail { get; set; } = null!;

    public bool IsSponsorActive { get; set; }

    public List<SendAlsoToDataModel> Recipients { get; set; } = [];
}

public class SendAlsoToDataModel
{
    public int Id { get; set; }

    public string RecipientName { get; set; } = null!;

    public string RecipientEmail { get; set; } = null!;

    public bool SendAsBcc { get; set; }
}