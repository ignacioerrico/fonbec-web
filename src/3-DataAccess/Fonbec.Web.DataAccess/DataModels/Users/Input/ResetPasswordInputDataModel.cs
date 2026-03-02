namespace Fonbec.Web.DataAccess.DataModels.Users.Input;

public class ResetPasswordInputDataModel
{
    public string UserId { get; set; } = null!;

    public string NewPassword { get; set; } = null!;
}