namespace Fonbec.Web.DataAccess.Entities.Enums;

public enum EducationLevel : byte
{
    PrimarySchool,
    SecondarySchool,
    University
}

public static class EducationLevelTranslator
{
    public static string EnumToString(this EducationLevel educationLevel) =>
        educationLevel switch
        {
            EducationLevel.PrimarySchool => "Primario",
            EducationLevel.SecondarySchool => "Secundario",
            EducationLevel.University => "Universitario",
            _ => "(Desconocido)"
        };
}