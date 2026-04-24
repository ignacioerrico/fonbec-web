using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.DataAccess.Entities.Abstract;

public abstract class UserWithoutAccount : Auditable
{
    public int Id { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string? NickName { get; set; }

    public Gender Gender { get; set; }

    public string? PhoneNumber { get; set; }

    public bool IsDeleted { get; set; }

    public int ChapterId { get; set; }
    public Chapter Chapter { get; set; } = null!;

    public string FullName() => $"{FirstName} {LastName}";
}