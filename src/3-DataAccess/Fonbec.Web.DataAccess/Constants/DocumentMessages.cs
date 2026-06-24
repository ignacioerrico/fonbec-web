namespace Fonbec.Web.DataAccess.Constants;

/// <summary>
/// Mensajes de error expuestos al usuario (español rioplatense).
/// </summary>
public static class DocumentMessages
{
    public const string StudentNotFoundOrInactive = "El estudiante no existe o no está activo.";

    public const string NoActivePlan = "No hay un plan activo.";

    public const string SponsorNotActiveForStudent = "El padrino no tiene un apadrinamiento activo con el estudiante.";

    public const string DuplicateLetter = "Ya existe una carta para este estudiante, padrino y plan.";

    public const string ReportCardCannotUseText = "Los boletines no pueden usar contenido de texto.";

    public const string NotAuthorizedDigitalImprovement = "No está autorizado para realizar mejora digital.";

    public const string NotAuthorizedToReview = "No está autorizado para revisar documentos.";

    public const string DocumentIsNotLetter = "El documento no es una carta.";

    public const string LetterConfirmationsRequired = "Debe completar todas las confirmaciones de la carta.";

    public const string DocumentIsNotReportCard = "El documento no es un boletín.";

    public const string ReportCardConfirmationsRequired = "Debe completar todas las confirmaciones del boletín.";

    public const string DocumentIsNotOther = "El documento no es de tipo 'otros'.";

    public const string RejectionReasonRequired = "Debe indicar un motivo de rechazo.";

    public const string NotAuthorizedReviewProgress = "No está autorizado para ver el progreso de revisión.";

    public const string NotAuthorizedLetterPlanProgress = "No está autorizado para ver el progreso del plan de cartas.";

    public const string AdminCannotUpload = "Los usuarios administradores no pueden subir documentos.";

    public const string NotAuthorizedToUpload = "No está autorizado para subir documentos.";

    public const string UploaderNotAssignedToStudent = "El mediador no está asignado a este estudiante.";

    public const string ManagerNotAuthorizedForChapter = "El coordinador no está autorizado para la filial de este estudiante.";

    public const string BlobContentRequired = "Debe proporcionar un archivo.";

    public const string YouTubeVideoIdRequired = "Debe proporcionar el identificador del video de YouTube.";

    public const string TextContentRequired = "Debe proporcionar el texto del documento.";

    public const string DocumentNotFoundOrImprovementLockNotHeld =
        "El documento no existe o no tiene el bloqueo de mejora digital.";

    public const string ConcurrencyConflict =
        "El documento fue modificado por otro usuario. Intente nuevamente.";

    public const string LetterNotFoundOrNotLockedForReview =
        "La carta no existe o no está bloqueada para revisión.";

    public const string ReportCardNotFoundOrNotLockedForReview =
        "El boletín no existe o no está bloqueado para revisión.";

    public const string DocumentNotFoundOrNotLockedForReview =
        "El documento no existe o no está bloqueado para revisión.";

    public const string DocumentTypeMismatch =
        "El tipo de documento no coincide con la operación solicitada.";
}
