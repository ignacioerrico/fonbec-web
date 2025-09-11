namespace Fonbec.Web.Logic.Models.Users.Output;

public record ValidateUniqueEmailOutputModel(
    bool IsEmailUnique,
    string? UserFullNameOrNull
);