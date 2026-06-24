namespace Fonbec.Web.Logic.Models.Results;

public record CrudResult<T>(T? Value = default, IReadOnlyList<string>? Errors = null)
{
    public bool IsSuccess => Errors is null or { Count: 0 };
}