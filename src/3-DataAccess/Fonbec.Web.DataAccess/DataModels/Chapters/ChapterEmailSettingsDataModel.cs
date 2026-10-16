using Fonbec.Web.DataAccess.Entities.Enums;

namespace Fonbec.Web.DataAccess.DataModels.Chapters;

public sealed class ChapterEmailSettingsDataModel
{
    public string ChapterName { get; init; } = string.Empty;

    public string SubjectPrefix { get; init; } = "FONBEC |";

    public IReadOnlyList<ChapterEmailTemplateDataModel> Templates { get; init; } = [];
}

public sealed class ChapterEmailTemplateDataModel
{
    public EmailMessageKind Kind { get; init; }

    public string Subject { get; init; } = string.Empty;

    public string Body { get; init; } = string.Empty;
}