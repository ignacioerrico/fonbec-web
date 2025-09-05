using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.DataAccess.DataModels.Users;

public class AllUsersDataModel
{
    public int UserId { get; set; }

    public string UserFirstName { get; set; } = null!;

    public string UserLastName { get; set; } = null!;

    public string? UserNickName { get; set; }

    public Gender UserGender { get; set; }

    public string? UserEmail { get; set; }

    public string? UserPhoneNumber { get; set; }

    public bool IsUserLockedOut { get; set; }

    public DateTimeOffset? UserLockOutEndsOnUtc { get; set; }
}