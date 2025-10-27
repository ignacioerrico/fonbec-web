using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.Ui.Models.User;

public class UserCreateBindModel
{
    public int ChapterCount { get; set; }

    public int UserChapterId { get; set; }

    public string UserFirstName { get; set; } = null!;

    public string UserLastName { get; set; } = null!;

    public string UserNickName { get; set; } = null!;

    public string UserRole { get; set; } = null!;

    public Gender UserGender { get; set; } = default!;

    public string UserEmail { get; set; } = null!;

    public string UserPhoneNumber { get; set; } = null!;

    public string UserNotes { get; set; } = null!;
}