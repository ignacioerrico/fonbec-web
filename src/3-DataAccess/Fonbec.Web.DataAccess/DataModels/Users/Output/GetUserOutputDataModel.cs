namespace Fonbec.Web.DataAccess.DataModels.Users.Output;

public class GetUserOutputDataModel
{
    public string UserFullName { get; init; } = null!;

    public string? UserNickName { get; init; }

    public string UserRole { get; set; } = null!;
}