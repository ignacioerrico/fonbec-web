namespace Fonbec.Web.DataAccess.DataModels.Users;

public record ChapterManagerContactDataModel(string Email, string FullName)
{
    public string FirstName { get; init; } = string.Empty;

    public string? NickName { get; init; }
}