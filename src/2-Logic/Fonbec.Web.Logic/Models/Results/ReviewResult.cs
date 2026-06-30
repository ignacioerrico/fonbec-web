namespace Fonbec.Web.Logic.Models.Results;

public record ReviewResult(bool IsSuccess, IReadOnlyList<string>? Errors = null);