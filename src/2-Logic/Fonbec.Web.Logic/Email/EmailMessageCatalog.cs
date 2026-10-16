using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.Logic.Email;

public sealed record EmailPlaceholderHelp(string Token, string Description);

public sealed class EmailMessageDefinition
{
    public required EmailMessageKind Kind { get; init; }

    public required string Name { get; init; }

    public required string WhenSent { get; init; }

    public required string Recipients { get; init; }

    public required string DefaultSubject { get; init; }

    public required string DefaultBody { get; init; }

    public required IReadOnlyList<EmailPlaceholderHelp> Placeholders { get; init; }
}

public static class EmailMessageCatalog
{
    public const string DefaultSubjectPrefix = EmailTemplateRenderer.DefaultSubjectPrefix;

    private static readonly EmailPlaceholderHelp Filial = new(
        "{filial}",
        "Nombre de la filial.");

    private static readonly EmailPlaceholderHelp NuevoDocumento = new(
        "{nuevo-documento}",
        "«nueva carta», «nuevo boletín», «nueva libreta universitaria» o «nuevo documento». Si la primera letra del marcador es mayúscula, el texto también empieza con mayúscula.");

    private static readonly EmailPlaceholderHelp UnNuevoDocumento = new(
        "{un-nuevo-documento}",
        "«una nueva carta», «un nuevo boletín», «una nueva libreta universitaria» o «un nuevo documento». Respeta la mayúscula inicial del marcador.");

    private static readonly EmailPlaceholderHelp Documento = new(
        "{documento}",
        "«carta», «boletín», «libreta universitaria» o «documento». Respeta la mayúscula inicial del marcador.");

    private static readonly EmailPlaceholderHelp ElDocumento = new(
        "{el-documento}",
        "«la carta», «el boletín», «la libreta universitaria» o «el documento». Respeta la mayúscula inicial del marcador.");

    private static readonly EmailPlaceholderHelp DocumentoRechazado = new(
        "{documento-rechazado}",
        "«carta rechazada», «boletín rechazado», «libreta universitaria rechazada» o «documento rechazado». Respeta la mayúscula inicial del marcador.");

    private static readonly EmailPlaceholderHelp Motivo = new(
        "{motivo-de-rechazo}",
        "Motivo por el que se rechazó el documento.");

    private static readonly EmailPlaceholderHelp PadrinoApodo = new(
        "{padrino-apodo}",
        "Apodo del padrino, o su nombre si no tiene apodo, o el nombre de la empresa. Siempre empieza con mayúscula.");

    private static readonly EmailPlaceholderHelp PadrinoNombre = new(
        "{padrino-nombre}",
        "Nombre del padrino, o el nombre de la empresa. Siempre empieza con mayúscula.");

    private static readonly EmailPlaceholderHelp PadrinoApellido = new(
        "{padrino-apellido}",
        "Apellido del padrino. Vacío si el remitente es una empresa. Siempre empieza con mayúscula.");

    private static readonly EmailPlaceholderHelp Ahijado = new(
        "{ahijado}",
        "«ahijado», «ahijada» o «ahijado/a».");

    private static readonly EmailPlaceholderHelp ElBecario = new(
        "{el-becario}",
        "«el becario», «la becaria» o «el becario/la becaria».");

    private static readonly EmailPlaceholderHelp BecarioApodo = new(
        "{becario-apodo}",
        "Apodo del becario, o su nombre si no tiene apodo. Siempre empieza con mayúscula.");

    private static readonly EmailPlaceholderHelp BecarioNombre = new(
        "{becario-nombre}",
        "Nombre del becario. Siempre empieza con mayúscula.");

    private static readonly EmailPlaceholderHelp BecarioApellido = new(
        "{becario-apellido}",
        "Apellido del becario. Siempre empieza con mayúscula.");

    private static readonly EmailPlaceholderHelp SuPadrino = new(
        "{su-padrino}",
        "«su padrino», «su madrina», «su padrino/su madrina» o «la empresa».");

    private static readonly EmailPlaceholderHelp Mensaje = new(
        "{mensaje-de-padrino}",
        "Texto del mensaje escrito por el padrino o la empresa.");

    private static readonly EmailPlaceholderHelp CoordinadorApodo = new(
        "{coordinador-apodo}",
        "Apodo del coordinador, o su nombre si no tiene apodo.");

    private static readonly EmailPlaceholderHelp Mediador = new(
        "{mediador}",
        "«mediador», «mediadora» o «mediador/mediadora».");

    private static readonly EmailPlaceholderHelp MediadorApodo = new(
        "{mediador-apodo}",
        "Apodo del mediador, o su nombre si no tiene apodo.");

    private static readonly EmailPlaceholderHelp PlanMes = new(
        "{plan-mes}",
        "Mes de la campaña, por ejemplo «septiembre». {Plan-mes} queda «Septiembre».");

    private static readonly EmailPlaceholderHelp PlanAnio = new(
        "{plan-año}",
        "Año de la campaña, por ejemplo «2026».");

    private static readonly EmailPlaceholderHelp EnlaceHistorial = new(
        "[Ver historial de documentos]{enlace-historial-documentos}",
        "Enlace al historial de documentos. El texto entre corchetes es el texto del enlace.");

    private static readonly EmailPlaceholderHelp EnlaceAvance = new(
        "[Ver avance de la campaña]{enlace-avance-campaña}",
        "Enlace al avance de la campaña. El texto entre corchetes es el texto del enlace.");

    private static readonly EmailPlaceholderHelp EnlaceBecarios = new(
        "[Ver Mis becarios]{enlace-mis-becarios}",
        "Enlace a Mis becarios. El texto entre corchetes es el texto del enlace.");

    private static readonly EmailPlaceholderHelp EnlaceRechazados = new(
        "[Ver documentos rechazados]{enlace-documentos-rechazados}",
        "Enlace a Documentos rechazados. El texto entre corchetes es el texto del enlace.");

    private static readonly EmailPlaceholderHelp EnlaceMensajes = new(
        "[Ver todos los mensajes para becarios]{enlace-mensajes-para-becarios}",
        "Enlace a Mensajes para becarios. El texto entre corchetes es el texto del enlace.");

    private static readonly EmailPlaceholderHelp EnlaceConfirmacion = new(
        "[acá]{enlace-confirmación-correo}",
        "Enlace para confirmar el correo. El texto entre corchetes es el texto del enlace.");

    private static readonly EmailPlaceholderHelp EnlaceRestablecer = new(
        "[acá]{enlace-restablecer-contraseña}",
        "Enlace para restablecer la contraseña. El texto entre corchetes es el texto del enlace.");

    private static readonly EmailPlaceholderHelp Usuario = new(
        "{usuario}",
        "Correo electrónico del usuario.");

    private static readonly EmailPlaceholderHelp Contrasena = new(
        "{contraseña}",
        "Contraseña generada.");

    public static IReadOnlyList<EmailMessageDefinition> All { get; } =
    [
        new()
        {
            Kind = EmailMessageKind.NewDocument,
            Name = "Nuevo documento",
            WhenSent = "Cuando se aprueba un documento, o cuando un coordinador comparte un documento que estaba retenido.",
            Recipients = "Al padrino o a la empresa, si tienen correo. Los destinatarios adicionales de un padrino van en copia o en copia oculta.",
            DefaultSubject = "{Nuevo-documento} disponible",
            DefaultBody = """
                Hola, {padrino-apodo}:

                Hay {un-nuevo-documento} disponible de tu {ahijado} {becario-apodo}.

                [Ver historial de documentos]{enlace-historial-documentos}

                FONBEC {filial}
                """,
            Placeholders = [Filial, NuevoDocumento, UnNuevoDocumento, Documento, ElDocumento, DocumentoRechazado, PadrinoApodo, PadrinoNombre, PadrinoApellido, Ahijado, ElBecario, BecarioApodo, BecarioNombre, BecarioApellido, EnlaceHistorial],
        },
        new()
        {
            Kind = EmailMessageKind.PlanReady,
            Name = "Campaña lista para completar",
            WhenSent = "Cuando aprobar o compartir una carta deja la campaña lista para completarse.",
            Recipients = "A cada coordinador de la filial.",
            DefaultSubject = "Campaña de {Plan-mes} de {plan-año} lista para completar",
            DefaultBody = """
                La campaña de {Plan-mes} de {plan-año} está lista para marcarse como completada.

                [Ver avance de la campaña]{enlace-avance-campaña}
                """,
            Placeholders = [Filial, PlanMes, PlanAnio, EnlaceAvance],
        },
        new()
        {
            Kind = EmailMessageKind.DocumentRejected,
            Name = "Documento rechazado",
            WhenSent = "Cuando se rechaza un documento y la filial lo devuelve al mediador, o cuando un coordinador devuelve un rechazo retenido.",
            Recipients = "Al mediador del becario.",
            DefaultSubject = "{Documento-rechazado}",
            DefaultBody = """
                Hola, {mediador} {mediador-apodo}:

                Se rechazó {el-documento} de {becario-apodo} {becario-apellido}.

                Motivo: {motivo-de-rechazo}

                Por favor, decile a {becario-apodo} que corrija el documento y subilo nuevamente al sistema.

                [Ver Mis becarios]{enlace-mis-becarios}
                """,
            Placeholders = [Filial, Documento, ElDocumento, DocumentoRechazado, NuevoDocumento, UnNuevoDocumento, Motivo, BecarioApodo, BecarioNombre, BecarioApellido, ElBecario, Ahijado, Mediador, MediadorApodo, EnlaceBecarios],
        },
        new()
        {
            Kind = EmailMessageKind.HeldRejection,
            Name = "Rechazo en espera de decisión",
            WhenSent = "Cuando se rechaza un documento y la filial retiene el rechazo para que lo decida un coordinador.",
            Recipients = "A cada coordinador de la filial.",
            DefaultSubject = "{Documento-rechazado} requiere la decisión de un coordinador",
            DefaultBody = """
                Hola, {coordinador-apodo}:

                Se rechazó {el-documento} de {becario-apodo} {becario-apellido}.
                Se requiere tu decisión.

                Motivo: {motivo-de-rechazo}

                [Ver documentos rechazados]{enlace-documentos-rechazados}
                """,
            Placeholders = [Filial, Documento, ElDocumento, DocumentoRechazado, NuevoDocumento, UnNuevoDocumento, Motivo, BecarioApodo, BecarioNombre, BecarioApellido, ElBecario, Ahijado, CoordinadorApodo, EnlaceRechazados],
        },
        new()
        {
            Kind = EmailMessageKind.RecipientMessage,
            Name = "Nuevo mensaje para un becario",
            WhenSent = "Cuando un padrino o una empresa escribe un mensaje desde el historial del becario.",
            Recipients = "Al mediador actual del becario.",
            DefaultSubject = "Nuevo mensaje para {becario-apodo} {becario-apellido}",
            DefaultBody = """
                Hola, {mediador} {mediador-apodo}:

                Hay un mensaje nuevo para {el-becario} {becario-apodo} {becario-apellido} de {su-padrino} {padrino-apodo}:

                {mensaje-de-padrino}

                Hacele llegar el mensaje a {becario-apodo} y marcalo como compartido en la aplicación.

                [Ver todos los mensajes para becarios]{enlace-mensajes-para-becarios}
                """,
            Placeholders = [Filial, ElBecario, BecarioApodo, BecarioNombre, BecarioApellido, SuPadrino, PadrinoApodo, PadrinoNombre, PadrinoApellido, Mensaje, Mediador, MediadorApodo, EnlaceMensajes],
        },
        new()
        {
            Kind = EmailMessageKind.NewAccount,
            Name = "Nueva cuenta",
            WhenSent = "Cuando se crea un usuario de esta filial.",
            Recipients = "Al usuario nuevo.",
            DefaultSubject = "Tu nueva cuenta de usuario",
            DefaultBody = """
                Usuario: {usuario}

                Contraseña: {contraseña}

                No compartas esta información con nadie. Nadie nunca va a pedirte tu contraseña.
                """,
            Placeholders = [Filial, Usuario, Contrasena],
        },
        new()
        {
            Kind = EmailMessageKind.NewPassword,
            Name = "Nueva contraseña",
            WhenSent = "Cuando se restablece la contraseña de un usuario de esta filial.",
            Recipients = "A ese usuario.",
            DefaultSubject = "Tu nueva contraseña",
            DefaultBody = """
                Usuario: {usuario}

                Contraseña nueva: {contraseña}

                No compartas esta información con nadie. Nadie nunca va a pedirte tu contraseña.
                """,
            Placeholders = [Filial, Usuario, Contrasena],
        },
        new()
        {
            Kind = EmailMessageKind.ConfirmEmail,
            Name = "Confirmación de correo",
            WhenSent = "Cuando un usuario pide verificar su correo o cambiarlo. Si cambia el correo, el mensaje va a la dirección nueva.",
            Recipients = "Al usuario.",
            DefaultSubject = "Confirmá tu correo electrónico",
            DefaultBody = """
                Confirmá tu correo electrónico haciendo clic [acá]{enlace-confirmación-correo}.
                """,
            Placeholders = [Filial, Usuario, EnlaceConfirmacion],
        },
        new()
        {
            Kind = EmailMessageKind.ResetPassword,
            Name = "Restablecer contraseña",
            WhenSent = "Cuando un usuario confirmado pide restablecer su contraseña.",
            Recipients = "A ese usuario.",
            DefaultSubject = "Restablecé tu contraseña",
            DefaultBody = """
                Restablecé tu contraseña haciendo clic [acá]{enlace-restablecer-contraseña}.
                """,
            Placeholders = [Filial, Usuario, EnlaceRestablecer],
        },
    ];

    public static EmailMessageDefinition Get(EmailMessageKind kind) =>
        All.Single(definition => definition.Kind == kind);
}