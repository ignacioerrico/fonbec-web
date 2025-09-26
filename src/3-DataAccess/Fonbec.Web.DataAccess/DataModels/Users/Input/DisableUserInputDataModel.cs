namespace Fonbec.Web.DataAccess.DataModels.Users.Input;

public class DisableUserInputDataModel
{
    public string UserIdToDisable { get; set; } = null!;

    public bool DisableUser { get; set; }

    public int ModifiedByUserId { get; set; }
}