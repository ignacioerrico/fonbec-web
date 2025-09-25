using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.DataAccess.DataModels.Users.Input;

public class CreateUserInputDataModel
{
    public int UserChapterId { get; set; }

    public string UserFirstName { get; set; } = null!;

    public string UserLastName { get; set; } = null!;

    public string? UserNickName { get; set; }

    public string UserRole { get; set; } = null!;

    public Gender UserGender { get; set; }

    public string UserEmail { get; set; } = null!;

    public string? UserPhoneNumber { get; set; }

    public int CreatedById { get; set; }

    public string GeneratedPassword { get; set; } = null!;
}