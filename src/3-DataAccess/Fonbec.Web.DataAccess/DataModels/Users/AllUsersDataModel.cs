using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.DataAccess.DataModels.Users;

public class AllUsersDataModel
{
    public List<AllUsersUserDataModel> Users { get; set; } = null!;

    public List<AllUsersUsersInRoleDataModel> UsersInRoles { get; set; } = new();
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

    public string? UserChapterName { get; set; }

    public bool CanUserBeLockedOut { get; set; }

    public DateTimeOffset? UserLockOutEndsOnUtc { get; set; }

    public string? CreatedByFullName { get; set; }
    public DateTime CreatedOnUtc { get; set; }

    public string? LastUpdatedByFullName { get; set; }
    public DateTime? LastUpdatedOnUtc { get; set; }

    public string? DisabledByFullName { get; set; }
    public DateTime? DisabledOnUtc { get; set; }

    public string? ReenabledByFullName { get; set; }
    public DateTime? ReenabledOnUtc { get; set; }
}

public class AllUsersUsersInRoleDataModel
{
    public string Role { get; set; } = null!;

    public IEnumerable<int> UserIdsInRole { get; set; } = null!;
}