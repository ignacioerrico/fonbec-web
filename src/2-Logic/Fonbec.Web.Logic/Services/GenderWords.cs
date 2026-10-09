using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.Logic.Services;

/// <summary>
/// Spanish nouns that change with <see cref="Gender"/>. Unknown uses both forms.
/// </summary>
public static class GenderWords
{
    public static string Becario(Gender gender) =>
        Choose(gender, "becario", "becaria");

    /// <summary>For example <c>el becario</c>, <c>la becaria</c>.</summary>
    public static string BecarioWithArticle(Gender gender) =>
        Choose(gender, "el becario", "la becaria", "el becario/la becaria");

    public static string Padrino(Gender? gender) =>
        Choose(gender, "padrino", "madrina");

    /// <summary>For example <c>su padrino</c>, <c>su madrina</c>.</summary>
    public static string SuPadrino(Gender? gender) =>
        Choose(gender, "su padrino", "su madrina", "su padrino/su madrina");

    public static string Ahijado(Gender gender) =>
        Choose(gender, "ahijado", "ahijada", "ahijado/a");

    private static string Choose(Gender? gender, string male, string female, string? unknown = null) =>
        gender switch
        {
            Gender.Male => male,
            Gender.Female => female,
            _ => unknown ?? $"{male}/{female}",
        };
}