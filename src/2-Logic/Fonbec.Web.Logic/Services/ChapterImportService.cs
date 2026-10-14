using System.Globalization;
using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Import;
using Fonbec.Web.Logic.Models.Sponsors.Input;
using Fonbec.Web.Logic.Models.Students.Input;
using Fonbec.Web.Logic.Util;

namespace Fonbec.Web.Logic.Services;

public interface IChapterImportService
{
    byte[] BuildExcelTemplate();

    byte[] BuildStudentsCsvTemplate();

    byte[] BuildSponsorsCsvTemplate();

    Task<ChapterImportResult> PreviewAsync(ChapterImportRequest request);

    Task<ChapterImportResult> CommitAsync(ChapterImportRequest request);
}

public class ChapterImportService(
    IStudentRepository studentRepository,
    ISponsorRepository sponsorRepository,
    IUserRepository userRepository,
    IStudentService studentService,
    ISponsorService sponsorService) : IChapterImportService
{
    public byte[] BuildExcelTemplate() => ChapterImportTemplates.Excel();

    public byte[] BuildStudentsCsvTemplate() => ChapterImportTemplates.StudentsCsv();

    public byte[] BuildSponsorsCsvTemplate() => ChapterImportTemplates.SponsorsCsv();

    public Task<ChapterImportResult> PreviewAsync(ChapterImportRequest request) =>
        ClassifyAsync(request, commit: false);

    public Task<ChapterImportResult> CommitAsync(ChapterImportRequest request) =>
        ClassifyAsync(request, commit: true);

    private async Task<ChapterImportResult> ClassifyAsync(ChapterImportRequest request, bool commit)
    {
        if (request.CallerChapterId is int callerChapterId && callerChapterId != request.ChapterId)
        {
            return FileError("Solo se puede importar en la filial del coordinador.");
        }

        if (request.ChapterId <= 0)
        {
            return FileError("Elegí una filial.");
        }

        var parsed = ChapterImportParser.Parse(request.Files);
        if (parsed.Students.Count == 0 && parsed.Sponsors.Count == 0)
        {
            return new ChapterImportResult
            {
                FileErrors = parsed.FileErrors.Count > 0
                    ? parsed.FileErrors
                    : ["El archivo no tiene filas para importar."],
            };
        }

        var existingStudents = await studentRepository.GetImportMatchKeysAsync(request.ChapterId);
        var existingSponsors = await sponsorRepository.GetImportEmailsAsync(request.ChapterId);
        var facilitators = await userRepository.GetActiveFacilitatorsAsync(request.ChapterId);

        var studentKeys = existingStudents
            .Select(student => StudentKey(student.FirstName, student.LastName))
            .ToHashSet(StringComparer.Ordinal);
        var sponsorEmails = existingSponsors
            .Where(email => !string.IsNullOrWhiteSpace(email))
            .Select(NormalizeEmail)
            .ToHashSet(StringComparer.Ordinal);
        var studentsInFile = new HashSet<string>(StringComparer.Ordinal);
        var sponsorsInFile = new HashSet<string>(StringComparer.Ordinal);
        var facilitatorsByEmail = facilitators
            .Where(facilitator => !string.IsNullOrWhiteSpace(facilitator.Email))
            .GroupBy(facilitator => NormalizeEmail(facilitator.Email))
            .ToDictionary(group => group.Key, group => group.First().Id, StringComparer.Ordinal);
        var facilitatorIds = facilitators.Select(facilitator => facilitator.Id).ToHashSet();
        var defaultFacilitatorId = request.DefaultFacilitatorId is > 0 ? request.DefaultFacilitatorId : null;

        var rows = new List<ClassifiedRow>();
        foreach (var student in parsed.Students)
        {
            rows.Add(ClassifyStudent(student, studentKeys, studentsInFile, facilitatorsByEmail, facilitatorIds, defaultFacilitatorId));
        }

        foreach (var sponsor in parsed.Sponsors)
        {
            rows.Add(ClassifySponsor(sponsor, sponsorEmails, sponsorsInFile));
        }

        if (commit)
        {
            foreach (var row in rows.Where(row => row.Result.Status == ChapterImportRowStatus.Create))
            {
                await CreateAsync(request, row);
            }
        }

        return new ChapterImportResult
        {
            FileErrors = parsed.FileErrors,
            Rows = rows.Select(row => row.Result).ToList(),
        };
    }

    private static ClassifiedRow ClassifyStudent(
        ChapterImportParsedRow row,
        HashSet<string> existingKeys,
        HashSet<string> keysInFile,
        Dictionary<string, int> facilitatorsByEmail,
        HashSet<int> facilitatorIds,
        int? defaultFacilitatorId)
    {
        var errors = new List<string>();
        RequireName(row, errors);
        CheckLength(row.FirstName, MaxLength.FonbecWebUser.FirstName, "El nombre", errors);
        CheckLength(row.LastName, MaxLength.FonbecWebUser.LastName, "El apellido", errors);
        CheckLength(row.NickName, MaxLength.FonbecWebUser.NickName, "El apodo", errors);
        CheckLength(row.Email, MaxLength.FonbecWebUser.Email, "El correo", errors);
        CheckLength(row.Phone, MaxLength.FonbecWebUser.PhoneNumber, "El teléfono", errors);
        CheckLength(row.Notes, MaxLength.Auditable.Notes, "Las notas", errors);
        AddContactErrors(row, errors);

        if (!TryParseGender(row.Gender, out var gender))
        {
            errors.Add("Género no reconocido.");
        }

        int? secondaryYear = null;
        int? universityYear = null;
        if (!TryParseYear(row.SecondaryYear, out secondaryYear))
        {
            errors.Add("El año de inicio de secundaria no es válido.");
        }

        if (!TryParseYear(row.UniversityYear, out universityYear))
        {
            errors.Add("El año de inicio de la universidad no es válido.");
        }

        if (secondaryYear is int secondary && universityYear is int university && university <= secondary)
        {
            errors.Add("La fecha de inicio de la universidad no puede ser igual o anterior a la del secundario.");
        }

        var facilitatorId = 0;
        if (!string.IsNullOrWhiteSpace(row.FacilitatorEmail))
        {
            if (!facilitatorsByEmail.TryGetValue(NormalizeEmail(row.FacilitatorEmail), out facilitatorId))
            {
                errors.Add("No hay un mediador con ese correo en la filial.");
            }
        }
        else if (defaultFacilitatorId is int selected && facilitatorIds.Contains(selected))
        {
            facilitatorId = selected;
        }
        else
        {
            errors.Add("Elegí un mediador de la filial.");
        }

        if (errors.Count > 0)
        {
            return new ClassifiedRow(row, Error(row, errors), gender, facilitatorId, secondaryYear, universityYear);
        }

        var key = StudentKey(row.FirstName, row.LastName);
        if (existingKeys.Contains(key))
        {
            return new ClassifiedRow(
                row,
                Skip(row, "Ya existe en la filial."),
                gender,
                facilitatorId,
                secondaryYear,
                universityYear);
        }

        if (!keysInFile.Add(key))
        {
            return new ClassifiedRow(
                row,
                Skip(row, "Ya está en el archivo."),
                gender,
                facilitatorId,
                secondaryYear,
                universityYear);
        }

        return new ClassifiedRow(row, Create(row), gender, facilitatorId, secondaryYear, universityYear);
    }

    private static ClassifiedRow ClassifySponsor(
        ChapterImportParsedRow row,
        HashSet<string> existingEmails,
        HashSet<string> emailsInFile)
    {
        var errors = new List<string>();
        RequireName(row, errors);
        if (string.IsNullOrWhiteSpace(row.Email))
        {
            errors.Add("El correo es obligatorio.");
        }

        CheckLength(row.FirstName, MaxLength.FonbecWebUser.FirstName, "El nombre", errors);
        CheckLength(row.LastName, MaxLength.FonbecWebUser.LastName, "El apellido", errors);
        CheckLength(row.NickName, MaxLength.FonbecWebUser.NickName, "El apodo", errors);
        CheckLength(row.Email, MaxLength.FonbecWebUser.Email, "El correo", errors);
        CheckLength(row.Phone, MaxLength.FonbecWebUser.PhoneNumber, "El teléfono", errors);
        CheckLength(row.Notes, MaxLength.Auditable.Notes, "Las notas", errors);
        AddContactErrors(row, errors);

        if (!TryParseGender(row.Gender, out var gender))
        {
            errors.Add("Género no reconocido.");
        }

        if (errors.Count > 0)
        {
            return new ClassifiedRow(row, Error(row, errors), gender, facilitatorId: 0, secondaryYear: null, universityYear: null);
        }

        var email = NormalizeEmail(row.Email);
        if (existingEmails.Contains(email))
        {
            return new ClassifiedRow(
                row,
                Skip(row, "Ya existe en la filial."),
                gender,
                facilitatorId: 0,
                secondaryYear: null,
                universityYear: null);
        }

        if (!emailsInFile.Add(email))
        {
            return new ClassifiedRow(
                row,
                Skip(row, "Ya está en el archivo."),
                gender,
                facilitatorId: 0,
                secondaryYear: null,
                universityYear: null);
        }

        return new ClassifiedRow(row, Create(row), gender, facilitatorId: 0, secondaryYear: null, universityYear: null);
    }

    private async Task CreateAsync(ChapterImportRequest request, ClassifiedRow row)
    {
        try
        {
            if (row.Parsed.Sheet == ChapterImportParser.StudentsSheet)
            {
                var result = await studentService.CreateStudentAsync(new CreateStudentInputModel(
                    request.ChapterId,
                    row.Parsed.FirstName,
                    row.Parsed.LastName,
                    row.Parsed.NickName,
                    row.Gender,
                    row.Parsed.Email,
                    row.Parsed.Phone,
                    row.Parsed.Notes,
                    YearDate(row.SecondaryYear),
                    YearDate(row.UniversityYear),
                    row.FacilitatorId,
                    request.CreatedById));

                Finish(row, result.IsSuccess && result.AnyAffectedRows, result.Errors, "No se pudo crear el becario.");
                return;
            }

            var sponsorResult = await sponsorService.CreateSponsorAsync(new CreateSponsorInputModel(
                request.ChapterId,
                row.Parsed.FirstName,
                row.Parsed.LastName,
                row.Parsed.NickName,
                row.Gender,
                row.Parsed.Email,
                row.Parsed.Phone,
                SponsorCompanyId: null,
                row.Parsed.Notes,
                request.CreatedById));

            Finish(row, sponsorResult.IsSuccess && sponsorResult.AnyAffectedRows, sponsorResult.Errors, "No se pudo crear el padrino.");
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            row.Result = row.Result with
            {
                Status = ChapterImportRowStatus.Failed,
                Detail = string.IsNullOrWhiteSpace(exception.Message)
                    ? "No se pudo crear."
                    : exception.Message,
            };
        }
    }

    private static void Finish(ClassifiedRow row, bool succeeded, IReadOnlyList<string>? errors, string fallback)
    {
        if (succeeded)
        {
            row.Result = row.Result with { Status = ChapterImportRowStatus.Created, Detail = null };
            return;
        }

        var detail = errors is { Count: > 0 } ? string.Join(' ', errors) : fallback;
        row.Result = row.Result with { Status = ChapterImportRowStatus.Failed, Detail = detail };
    }

    private static void RequireName(ChapterImportParsedRow row, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(row.FirstName))
        {
            errors.Add("El nombre es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(row.LastName))
        {
            errors.Add("El apellido es obligatorio.");
        }
    }

    private static void CheckLength(string value, int max, string label, List<string> errors)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length > max)
        {
            errors.Add($"{label} supera los {max} caracteres.");
        }
    }

    private static void AddContactErrors(ChapterImportParsedRow row, List<string> errors)
    {
        if (!string.IsNullOrWhiteSpace(row.Email) && ContactFieldValidator.ValidateEmail(row.Email) is string emailError)
        {
            errors.Add(emailError);
        }

        if (ContactFieldValidator.ValidatePhone(row.Phone) is string phoneError)
        {
            errors.Add(phoneError);
        }
    }

    private static bool TryParseGender(string value, out Gender gender)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            gender = Gender.Male;
            return true;
        }

        switch (ChapterImportParser.HeaderKey(value))
        {
            case "masculino":
            case "m":
            case "hombre":
            case "male":
                gender = Gender.Male;
                return true;
            case "femenino":
            case "f":
            case "mujer":
            case "female":
                gender = Gender.Female;
                return true;
            default:
                gender = Gender.Unknown;
                return false;
        }
    }

    private static bool TryParseYear(string value, out int? year)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            year = null;
            return true;
        }

        var text = value.Trim();
        var separator = text.IndexOfAny(['.', ',']);
        if (separator > 0)
        {
            text = text[..separator];
        }

        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            year = null;
            return false;
        }

        var maxYear = DateTime.UtcNow.Year + ChapterImportLimits.MaxYearOffset;
        if (parsed < ChapterImportLimits.MinYear || parsed > maxYear)
        {
            year = null;
            return false;
        }

        year = parsed;
        return true;
    }

    private static DateTime? YearDate(int? year) =>
        year is int value ? new DateTime(value, 1, 1) : null;

    private static string StudentKey(string firstName, string lastName) =>
        $"{firstName.NormalizeText()}\u001f{lastName.NormalizeText()}";

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string DisplayName(ChapterImportParsedRow row)
    {
        var name = $"{row.FirstName.Trim()} {row.LastName.Trim()}".Trim();
        if (name.Length > 0)
        {
            return name;
        }

        return string.IsNullOrWhiteSpace(row.Email) ? "(sin nombre)" : row.Email.Trim();
    }

    private static ChapterImportRowResult Create(ChapterImportParsedRow row) =>
        new(row.Sheet, row.RowNumber, DisplayName(row), ChapterImportRowStatus.Create, null);

    private static ChapterImportRowResult Skip(ChapterImportParsedRow row, string detail) =>
        new(row.Sheet, row.RowNumber, DisplayName(row), ChapterImportRowStatus.Skip, detail);

    private static ChapterImportRowResult Error(ChapterImportParsedRow row, List<string> errors) =>
        new(row.Sheet, row.RowNumber, DisplayName(row), ChapterImportRowStatus.Error, string.Join(' ', errors));

    private static ChapterImportResult FileError(string message) =>
        new() { FileErrors = [message] };

    private sealed class ClassifiedRow(
        ChapterImportParsedRow parsed,
        ChapterImportRowResult result,
        Gender gender,
        int facilitatorId,
        int? secondaryYear,
        int? universityYear)
    {
        public ChapterImportParsedRow Parsed { get; } = parsed;

        public ChapterImportRowResult Result { get; set; } = result;

        public Gender Gender { get; } = gender;

        public int FacilitatorId { get; } = facilitatorId;

        public int? SecondaryYear { get; } = secondaryYear;

        public int? UniversityYear { get; } = universityYear;
    }
}