using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.DataAccess.DataModels.Users;

public class AllUsersDataModel
{
    public List<AllUsersUserDataModel> Users { get; set; } = null!;

    public List<AllUsersUsersInRolesDataModel> UsersInRoles { get; set; } = new();
}

public class AllUsersUserDataModel
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

public class AllUsersUsersInRolesDataModel
{
    public string Role { get; set; } = null!;

    public IEnumerable<int> UserIdsInRole { get; set; } = null!;
}