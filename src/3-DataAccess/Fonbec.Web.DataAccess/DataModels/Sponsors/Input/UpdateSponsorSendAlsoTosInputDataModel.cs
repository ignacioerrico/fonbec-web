namespace Fonbec.Web.DataAccess.DataModels.Sponsors.Input;

public class UpdateSponsorSendAlsoTosInputDataModel
{
    public int SponsorId { get; set; }

    public int? ChapterId { get; set; }

    public int UpdatedById { get; set; }

    public List<UpdateSendAlsoToInputDataModel> Recipients { get; set; } = [];
}

public class UpdateSendAlsoToInputDataModel
{
    public int Id { get; set; }

    public string RecipientName { get; set; } = null!;

    public string RecipientEmail { get; set; } = null!;

    public bool SendAsBcc { get; set; }
}