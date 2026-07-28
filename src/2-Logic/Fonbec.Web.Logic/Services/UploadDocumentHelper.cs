using System.Globalization;
using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.Logic.Services;

/// <summary>
/// Shared helpers for the facilitator and manager document upload services (single source of
/// truth for the document-type query values, the period label, and the education-level rule).
/// </summary>
public static class UploadDocumentHelper
{
    public const string TipoCarta = "carta";
    public const string TipoBoletin = "boletin";
    public const string TipoOtro = "otro";

    private static readonly CultureInfo EsAr = CultureInfo.GetCultureInfo("es-AR");

    public static DocumentType? ParseDocumentType(string? documentType) =>
        documentType?.Trim().ToLowerInvariant() switch
        {
            TipoCarta => DocumentType.Letter,
            TipoBoletin => DocumentType.ReportCard,
            TipoOtro => DocumentType.Other,
            _ => null,
        };

    public static string FormatPeriod(DateTime startsOn)
    {
        var label = startsOn.ToString("MMM yyyy", EsAr).Replace(".", string.Empty);
        return EsAr.TextInfo.ToTitleCase(label);
    }

    public static EducationLevel ResolveEducationLevel(DateTime? secondarySchoolStartYear, DateTime? universityStartYear)
    {
        var now = DateTime.UtcNow;
        if (universityStartYear <= now)
        {
            return EducationLevel.University;
        }

        return secondarySchoolStartYear <= now
            ? EducationLevel.SecondarySchool
            : EducationLevel.PrimarySchool;
    }
}