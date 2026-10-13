namespace Fonbec.Web.DataAccess.DataModels.Users;

public sealed class FacilitatorStudentDataModel
{
    public int FacilitatorId { get; init; }

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;
}