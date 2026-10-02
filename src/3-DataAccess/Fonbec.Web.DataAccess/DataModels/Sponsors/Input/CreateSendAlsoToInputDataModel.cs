namespace Fonbec.Web.DataAccess.DataModels.Sponsors.Input;

public class CreateSendAlsoToInputDataModel
{
    public string RecipientName { get; set; } = null!;

    public string RecipientEmail { get; set; } = null!;

    public bool SendAsBcc { get; set; }
}