namespace Fonbec.Web.Logic.Models.Results;

public record CrudResult(int AffectedRows = 0, IReadOnlyList<string>? Errors = null)
{
    public bool AnyAffectedRows => AffectedRows > 0;

    public bool IsSuccess => Errors is null or { Count: 0 };
}