using Fonbec.Web.DataAccess.DataModels.Chapters;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;

namespace Fonbec.Web.Logic.Email;

public sealed record ComposedEmail(string Subject, string HtmlBody);

public interface IEmailComposer
{
    Task<ComposedEmail> ComposeAsync(
        int? chapterId,
        EmailMessageKind kind,
        IReadOnlyDictionary<string, string?> values,
        CancellationToken cancellationToken = default);
}

public sealed class EmailComposer(IChapterEmailSettingsRepository? repository = null) : IEmailComposer
{
    public static EmailComposer Defaults { get; } = new(null);

    public async Task<ComposedEmail> ComposeAsync(
        int? chapterId,
        EmailMessageKind kind,
        IReadOnlyDictionary<string, string?> values,
        CancellationToken cancellationToken = default)
    {
        ChapterEmailSettingsDataModel? settings = null;
        if (chapterId is int id && repository is not null)
        {
            settings = await repository.GetAsync(id, cancellationToken);
        }

        var definition = EmailMessageCatalog.Get(kind);
        var saved = settings?.Templates.FirstOrDefault(template => template.Kind == kind);
        var subjectTemplate = string.IsNullOrWhiteSpace(saved?.Subject) ? definition.DefaultSubject : saved.Subject;
        var bodyTemplate = string.IsNullOrWhiteSpace(saved?.Body) ? definition.DefaultBody : saved.Body;
        var merged = new Dictionary<string, string?>(values, StringComparer.OrdinalIgnoreCase)
        {
            ["filial"] = settings?.ChapterName ?? values.GetValueOrDefault("filial") ?? string.Empty,
        };

        return new ComposedEmail(
            EmailTemplateRenderer.ApplySubjectPrefix(
                settings?.SubjectPrefix ?? EmailTemplateRenderer.DefaultSubjectPrefix,
                EmailTemplateRenderer.RenderSubject(subjectTemplate, merged)),
            EmailTemplateRenderer.RenderBody(bodyTemplate, merged));
    }
}