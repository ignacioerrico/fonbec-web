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

    public const string NotAuthorizedToDisableUser =
        "No estás autorizado para deshabilitar o habilitar este usuario.";

    public const string CannotDisableRole =
        "Solo podés deshabilitar o habilitar mediadores y revisores.";

    public const string CannotDisableUserOutsideChapter =
        "No podés deshabilitar o habilitar usuarios de otra filial.";

    public const string CannotDisableFacilitatorWithStudents =
        "No se puede deshabilitar un mediador que tiene becarios asignados.";
}