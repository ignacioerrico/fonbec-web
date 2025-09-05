using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.DataAccess.DataModels.Users.Input;

public class UpdateUserInputDataModel
{
    public string UserId { get; set; } = null!;

    public string UserFirstName { get; set; } = null!;

    public string UserLastName { get; set; } = null!;

    public string? UserNickName { get; set; }

    public Gender Gender { get; set; }

    public string UserEmail { get; set; } = null!;

    public string? UserPhoneNumber { get; set; }

    public int UpdatedById { get; set; }
}