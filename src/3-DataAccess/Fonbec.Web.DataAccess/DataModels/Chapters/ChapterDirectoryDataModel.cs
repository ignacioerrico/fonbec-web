namespace Fonbec.Web.DataAccess.DataModels.Chapters;

public class ChapterDirectoryDataModel
{
    public List<ChapterStaffMemberDataModel> Staff { get; init; } = [];

    public Dictionary<int, int> StudentCounts { get; init; } = [];

    public Dictionary<int, int> SponsorCounts { get; init; } = [];
}

public class ChapterStaffMemberDataModel
{
    public int ChapterId { get; set; }

    public int UserId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? NickName { get; set; }

    public string Email { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public string Role { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public bool CanBeLockedOut { get; set; }

    public bool HasLinkedStudents { get; set; }
}