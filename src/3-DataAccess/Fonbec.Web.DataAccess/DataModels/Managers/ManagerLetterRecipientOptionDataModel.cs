namespace Fonbec.Web.DataAccess.DataModels.Managers;

/// <summary>A candidate recipient (sponsor or company) for a manager backup letter upload.</summary>
public class ManagerLetterRecipientOptionDataModel
{
    public int? SponsorId { get; init; }

    public int? CompanyId { get; init; }

    public string RecipientName { get; init; } = string.Empty;
}
