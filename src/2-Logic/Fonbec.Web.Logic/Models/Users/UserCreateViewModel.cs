using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.Logic.Models.Users;

public class UserCreateViewModel
{
    public string UserFirstName { get; set; } = null!;

    public string UserLastName { get; set; } = null!;

    public string UserNickName { get; set; } = null!;

    public List<string> UserRoles { get; set; } = [];

    public Gender UserGender { get; set; } = default!;

    public string UserEmail { get; set; } = null!;

    public string UserPhoneNumber { get; set; } = null!;
}