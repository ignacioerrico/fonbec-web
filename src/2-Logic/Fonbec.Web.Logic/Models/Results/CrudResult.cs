namespace Fonbec.Web.Logic.Models.Results;

public record CrudResult(int AffectedRows = 0, string? Message = null)
{
    public bool AnyAffectedRows => AffectedRows > 0;
}