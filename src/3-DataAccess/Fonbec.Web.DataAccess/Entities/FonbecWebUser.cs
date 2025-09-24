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

    [PersonalData]
    public string? Notes { get; set; }

    /// <summary>
    /// Branch Office this user belongs to; set to <value>null</value> to make it a global user.
    /// </summary>
    public int? ChapterId { get; set; }
    public Chapter? Chapter { get; set; }

    /// <summary>
    /// Users with role of 'Uploader' who are responsible for students will be linked to students as their facilitators.
    /// </summary>
    public List<Student>? Students { get; set; }

    public string FullName() => $"{FirstName} {LastName}";

    #region Audit Properties

    public int? CreatedById { get; set; }
    public FonbecWebUser? CreatedBy { get; set; } = null!;
    public DateTime CreatedOnUtc { get; set; }

    public int? LastUpdatedById { get; set; }
    public FonbecWebUser? LastUpdatedBy { get; set; } = null!;
    public DateTime? LastUpdatedOnUtc { get; set; }

    public int? DisabledById { get; set; }
    public FonbecWebUser? DisabledBy { get; set; } = null!;
    public DateTime? DisabledOnUtc { get; set; }

    public int? ReenabledById { get; set; }
    public FonbecWebUser? ReenabledBy { get; set; } = null!;
    public DateTime? ReenabledOnUtc { get; set; }

    #endregion
}