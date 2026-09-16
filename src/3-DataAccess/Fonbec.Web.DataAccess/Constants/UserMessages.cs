namespace Fonbec.Web.DataAccess.Constants;

/// <summary>
/// Mensajes de error de usuarios expuestos al usuario (español rioplatense).
/// </summary>
public static class UserMessages
{
    public const string UserNotFound = "El usuario no existe.";

    public const string NotAuthorizedToManageDigitalImprovement =
        "No estás autorizado para modificar el permiso de mejora de imágenes.";

    public const string CannotGrantDigitalImprovementToRole =
        "Este permiso solo se puede asignar a revisores y coordinadores.";

    public const string CannotGrantDigitalImprovementOutsideChapter =
        "No podés modificar este permiso para usuarios de otra filial.";
}