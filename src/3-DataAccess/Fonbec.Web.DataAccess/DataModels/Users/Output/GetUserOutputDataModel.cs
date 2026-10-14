using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.DataAccess.DataModels.Users.Output;

public class GetUserOutputDataModel
{
    public int? ChapterId { get; set; }

    public string UserFullName { get; init; } = null!;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? UserNickName { get; init; }

    public Gender Gender { get; set; }

    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; }

    public bool CanBeLockedOut { get; set; }

    public string UserRole { get; set; } = null!;
}