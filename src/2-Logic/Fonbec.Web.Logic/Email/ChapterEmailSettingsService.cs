using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.DataModels.Chapters;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;

namespace Fonbec.Web.Logic.Email;

public sealed class ChapterEmailTemplateEditor
{
    public EmailMessageKind Kind { get; init; }

    public string Name { get; init; } = string.Empty;

    public string WhenSent { get; init; } = string.Empty;

    public string Recipients { get; init; } = string.Empty;

    public string DefaultSubject { get; init; } = string.Empty;

    public string DefaultBody { get; init; } = string.Empty;

    public IReadOnlyList<EmailPlaceholderHelp> Placeholders { get; init; } = [];

    public string Subject { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;
}

public sealed class ChapterEmailSettingsEditor
{
    public string SubjectPrefix { get; set; } = EmailMessageCatalog.DefaultSubjectPrefix;

    public List<ChapterEmailTemplateEditor> Messages { get; set; } = [];
}

public interface IChapterEmailSettingsService
{
    Task<ChapterEmailSettingsEditor?> GetEditorAsync(int chapterId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> SaveAsync(
        int chapterId,
        int updatedById,
        string subjectPrefix,
        IReadOnlyList<ChapterEmailTemplateDataModel> templates,
        CancellationToken cancellationToken = default);
}

public sealed class ChapterEmailSettingsService(IChapterEmailSettingsRepository repository) : IChapterEmailSettingsService
{
    public async Task<ChapterEmailSettingsEditor?> GetEditorAsync(
        int chapterId, CancellationToken cancellationToken = default)
    {
        var settings = await repository.GetAsync(chapterId, cancellationToken);
        if (settings is null)
        {
            return null;
        }

        var saved = settings.Templates.ToDictionary(template => template.Kind);
        return new ChapterEmailSettingsEditor
        {
            SubjectPrefix = settings.SubjectPrefix,
            Messages = EmailMessageCatalog.All.Select(definition =>
            {
                saved.TryGetValue(definition.Kind, out var template);
                return new ChapterEmailTemplateEditor
                {
                    Kind = definition.Kind,
                    Name = definition.Name,
                    WhenSent = definition.WhenSent,
                    Recipients = definition.Recipients,
                    DefaultSubject = definition.DefaultSubject,
                    DefaultBody = definition.DefaultBody,
                    Placeholders = definition.Placeholders,
                    Subject = string.IsNullOrWhiteSpace(template?.Subject) ? definition.DefaultSubject : template.Subject,
                    Body = string.IsNullOrWhiteSpace(template?.Body) ? definition.DefaultBody : template.Body,
                };
            }).ToList(),
        };
    }

    public async Task<IReadOnlyList<string>> SaveAsync(
        int chapterId,
        int updatedById,
        string subjectPrefix,
        IReadOnlyList<ChapterEmailTemplateDataModel> templates,
        CancellationToken cancellationToken = default)
    {
        var errors = Validate(subjectPrefix, templates);
        if (errors.Count > 0)
        {
            return errors;
        }

        var saved = await repository.SaveAsync(
            chapterId,
            updatedById,
            subjectPrefix,
            templates,
            cancellationToken);
        return saved ? [] : ["No se encontró la filial."];
    }

    private static List<string> Validate(string subjectPrefix, IReadOnlyList<ChapterEmailTemplateDataModel> templates)
    {
        var errors = new List<string>();
        if (subjectPrefix.Trim().Length > MaxLength.Chapter.EmailSubjectPrefix)
        {
            errors.Add($"El texto del asunto no puede superar los {MaxLength.Chapter.EmailSubjectPrefix} caracteres.");
        }

        var byKind = templates.ToDictionary(template => template.Kind);
        foreach (var definition in EmailMessageCatalog.All)
        {
            if (!byKind.TryGetValue(definition.Kind, out var template)
                || string.IsNullOrWhiteSpace(template.Subject)
                || string.IsNullOrWhiteSpace(template.Body))
            {
                errors.Add($"Completá el asunto y el cuerpo de «{definition.Name}».");
                continue;
            }

            if (template.Subject.Trim().Length > MaxLength.ChapterEmailTemplate.Subject)
            {
                errors.Add($"El asunto de «{definition.Name}» no puede superar los {MaxLength.ChapterEmailTemplate.Subject} caracteres.");
            }

            if (template.Body.Trim().Length > MaxLength.ChapterEmailTemplate.Body)
            {
                errors.Add($"El cuerpo de «{definition.Name}» no puede superar los {MaxLength.ChapterEmailTemplate.Body} caracteres.");
            }
        }

        return errors;
    }
}