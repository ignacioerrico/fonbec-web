namespace Fonbec.Web.Logic.Models.Companies;

public static class CompanyFieldValidator
{
    public static bool IsValidName(string? name) =>
        !string.IsNullOrWhiteSpace(name) && name.Any(char.IsLetter);
}