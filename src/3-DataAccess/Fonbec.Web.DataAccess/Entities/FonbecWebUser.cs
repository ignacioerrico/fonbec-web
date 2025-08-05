using Fonbec.Web.DataAccess.Entities.Enums;
using Microsoft.AspNetCore.Identity;

namespace Fonbec.Web.DataAccess.Entities;

public class FonbecWebUser : IdentityUser<int>
{
    [PersonalData]
    public string FirstName { get; set; } = string.Empty;

    [PersonalData]
    public string LastName { get; set; } = string.Empty;

    [PersonalData]
    public string? NickName { get; set; }

    [PersonalData]
    public Gender Gender { get; set; }
}