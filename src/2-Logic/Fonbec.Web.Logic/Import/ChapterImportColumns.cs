namespace Fonbec.Web.Logic.Import;

public static class ChapterImportColumns
{
    public const string FirstName = "Nombre";
    public const string LastName = "Apellido";
    public const string NickName = "Apodo";
    public const string Gender = "Género";
    public const string Email = "Email";
    public const string Phone = "Teléfono";
    public const string Notes = "Notas";
    public const string SecondaryYear = "Inicio secundaria";
    public const string UniversityYear = "Inicio universidad";
    public const string Facilitator = "Mediador";

    public static readonly string[] Students =
    [
        FirstName,
        LastName,
        NickName,
        Gender,
        Email,
        Phone,
        Notes,
        SecondaryYear,
        UniversityYear,
        Facilitator,
    ];

    public static readonly string[] Sponsors =
    [
        FirstName,
        LastName,
        NickName,
        Gender,
        Email,
        Phone,
        Notes,
    ];
}

public static class ChapterImportLimits
{
    public const int MaxFileBytes = 2 * 1024 * 1024;

    public const int MaxRows = 2000;

    public const int MinYear = 1950;

    public const int MaxYearOffset = 30;
}