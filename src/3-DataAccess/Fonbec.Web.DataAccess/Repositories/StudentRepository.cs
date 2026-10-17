using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.DataModels.Import;
using Fonbec.Web.DataAccess.DataModels.Review;
using Fonbec.Web.DataAccess.DataModels.Students;
using Fonbec.Web.DataAccess.DataModels.Students.Input;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Repositories;

public interface IStudentRepository
{
    Task<List<AllStudentsDataModel>> GetAllStudentsAsync(int? chapterId);

    /// <summary>
    /// Scores of approved letters. <paramref name="chapterId"/> null includes every chapter.
    /// </summary>
    Task<List<StudentLetterScoreDataModel>> GetApprovedLetterScoresAsync(int? chapterId);

    /// <summary>
    /// Active students visible on the students list. <paramref name="chapterId"/> null counts every chapter.
    /// </summary>
    Task<int> CountStudentsAsync(int? chapterId);

    /// <summary>
    /// Active students in the same set as <see cref="CountStudentsAsync"/> who would show
    /// "Sin padrino": no current or upcoming sponsorship with an active padrino or empresa.
    /// A finished sponsorship does not count.
    /// </summary>
    Task<int> CountStudentsWithoutSponsorAsync(int? chapterId, DateTime asOf);

    Task<int?> GetStudentChapterIdAsync(int studentId);
    Task<int> CreateStudentAsync(CreateStudentInputDataModel inputDataModel);
    Task<int> UpdateStudentAsync(UpdateStudentInputDataModel dataModel);

    /// <summary>
    /// Returns <c>FirstName LastName</c> for an active student in the given chapter,
    /// or <c>null</c> when the student is missing, inactive, deleted, or in another chapter.
    /// </summary>
    Task<string?> GetActiveStudentDisplayNameInChapterAsync(int studentId, int chapterId);

    /// <summary>
    /// Returns an active student and their chapter.
    /// When <paramref name="chapterId"/> is set, the student must belong to that chapter.
    /// </summary>
    Task<ActiveStudentDataModel?> GetActiveStudentAsync(int studentId, int? chapterId);

    /// <summary>Get a single student's name, or <c>null</c> when the student does not exist.</summary>
    Task<CandidateNameDataModel?> GetStudentNameAsync(int studentId);

    /// <summary>
    /// Get every active student name except <paramref name="excludeStudentId"/>, ordered by id.
    /// Not chapter-restricted. The review picker samples this pool deterministically.
    /// </summary>
    Task<List<CandidateNameDataModel>> GetStudentCandidateNamesAsync(int excludeStudentId);

    /// <summary>
    /// Names of students in the chapter who are not deleted, including inactive ones.
    /// Used to skip people who already exist when importing.
    /// </summary>
    Task<List<StudentImportMatchDataModel>> GetImportMatchKeysAsync(int chapterId);
}

public class StudentRepository(IDbContextFactory<FonbecWebDbContext> dbContext,
    UserManager<FonbecWebUser> userManager) : IStudentRepository
{
    public async Task<List<AllStudentsDataModel>> GetAllStudentsAsync(int? chapterId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var allStudents = await db.Students
            .AsNoTracking()
            .Include(s => s.Facilitator)
            .Include(s => s.CreatedBy)
            .Include(s => s.LastUpdatedBy)
            .Include(s => s.DisabledBy)
            .Include(s => s.ReenabledBy)
            .Include(s => s.Chapter)
            .Where(s => s.IsActive
                        && (chapterId == null || chapterId == s.ChapterId))
            .Select(s => new AllStudentsDataModel(s)
            {
                ChapterId = s.ChapterId,
                StudentId = s.Id,
                StudentFirstName = s.FirstName,
                StudentLastName = s.LastName,
                StudentNickName = s.NickName,
                StudentGender = s.Gender,
                IsStudentActive = s.IsActive,
                FacilitatorId = s.Facilitator.Id,
                FacilitatorFirstName = s.Facilitator.FirstName,
                FacilitatorLastName = s.Facilitator.LastName,
                FacilitatorEmail = s.Facilitator.Email,
                StudentEmail = s.Email,
                Notes = s.Notes,
                StudentCurrentEducationLevel = s.CurrentEducationLevel,
                StudentSecondarySchoolStartYear = s.SecondarySchoolStartYear,
                StudentUniversityStartYear = s.UniversityStartYear,
                StudentPhoneNumber = s.PhoneNumber,
                StudentChapterName = s.Chapter.Name,
                ActiveSponsors = s.Sponsorships
                    .Where(sp => sp.IsActive
                                 && ((sp.SponsorId != null
                                      && sp.Sponsor != null
                                      && sp.Sponsor.IsActive
                                      && !sp.Sponsor.IsDeleted)
                                     || (sp.CompanyId != null
                                         && sp.Company != null
                                         && sp.Company.IsActive)))
                    .Select(sp => new StudentActiveSponsorDataModel
                    {
                        IsCompany = sp.CompanyId != null,
                        Name = sp.CompanyId != null && sp.Company != null
                            ? sp.Company.Name
                            : sp.Sponsor != null
                                ? sp.Sponsor.FirstName + " " + sp.Sponsor.LastName
                                : string.Empty,
                        StartDate = sp.StartDate,
                        EndDate = sp.EndDate,
                    })
                    .ToList(),
            })
            .OrderBy(s => s.StudentFirstName)
            .ThenBy(s => s.StudentLastName)
            .ToListAsync();

        return allStudents;
    }

    public async Task<List<StudentLetterScoreDataModel>> GetApprovedLetterScoresAsync(int? chapterId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        return await db.Set<Letter>()
            .AsNoTracking()
            .Where(letter => letter.Status == DocumentStatus.Approved && letter.Review != null)
            .Where(letter => chapterId == null || letter.ChapterId == chapterId)
            .Select(letter => new StudentLetterScoreDataModel
            {
                StudentId = letter.StudentId,
                PlanId = letter.PlanId,
                PlanStartsOn = letter.Plan.StartsOn,
                SpellingScore = letter.Review!.Assessment.SpellingScore,
                PenmanshipScore = letter.Review.Assessment.PenmanshipScore,
                ContentScore = letter.Review.Assessment.ContentScore,
            })
            .ToListAsync();
    }

    public async Task<int> CountStudentsAsync(int? chapterId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        return await db.Students
            .AsNoTracking()
            .CountAsync(s => s.IsActive
                             && (chapterId == null || chapterId == s.ChapterId));
    }

    public async Task<int> CountStudentsWithoutSponsorAsync(int? chapterId, DateTime asOf)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        return await db.Students
            .AsNoTracking()
            .CountAsync(s => s.IsActive
                             && (chapterId == null || chapterId == s.ChapterId)
                             && !s.Sponsorships.Any(sp =>
                                 sp.IsActive
                                 && (sp.EndDate == null || sp.EndDate >= asOf)
                                 && ((sp.SponsorId != null
                                      && sp.Sponsor != null
                                      && sp.Sponsor.IsActive
                                      && !sp.Sponsor.IsDeleted)
                                     || (sp.CompanyId != null
                                         && sp.Company != null
                                         && sp.Company.IsActive))));
    }

    public async Task<int?> GetStudentChapterIdAsync(int studentId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        return await db.Students
            .AsNoTracking()
            .Where(s => s.Id == studentId && !s.IsDeleted)
            .Select(s => (int?)s.ChapterId)
            .FirstOrDefaultAsync();
    }

    public async Task<int> CreateStudentAsync(CreateStudentInputDataModel inputDataModel)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        await EnsureValidFacilitatorAsync(inputDataModel.FacilitatorId, inputDataModel.ChapterId);

        var creator = await userManager.FindByIdAsync(inputDataModel.CreatedById.ToString())
                      ?? throw new InvalidOperationException("El usuario que crea el becario no existe.");

        if (creator.ChapterId is { } creatorChapterId && creatorChapterId != inputDataModel.ChapterId)
        {
            throw new InvalidOperationException("El becario debe crearse en la filial del coordinador.");
        }

        var student = new Student
        {
            ChapterId = inputDataModel.ChapterId,
            FirstName = inputDataModel.StudentFirstName,
            LastName = inputDataModel.StudentLastName,
            NickName = inputDataModel.StudentNickName,
            Gender = inputDataModel.StudentGender,
            Email = inputDataModel.StudentEmail,
            PhoneNumber = inputDataModel.StudentPhoneNumber,
            Notes = inputDataModel.StudentNotes,
            SecondarySchoolStartYear = inputDataModel.StudentSecondarySchoolStartYear,
            UniversityStartYear = inputDataModel.StudentUniversityStartYear,
            FacilitatorId = inputDataModel.FacilitatorId,
            CreatedById = inputDataModel.CreatedById
        };

        db.Students.Add(student);
        return await db.SaveChangesAsync();
    }

    public async Task<int> UpdateStudentAsync(UpdateStudentInputDataModel dataModel)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var studentDb = await db.Students.FindAsync(dataModel.StudentId);

        if (studentDb is not { IsActive: true })
        {
            return 0;
        }

        var updater = await userManager.FindByIdAsync(dataModel.UpdatedById.ToString())
                      ?? throw new InvalidOperationException("El usuario que modifica el becario no existe.");

        if (updater.ChapterId is { } updaterChapterId && updaterChapterId != studentDb.ChapterId)
        {
            throw new InvalidOperationException("El becario debe pertenecer a la filial del coordinador.");
        }

        await EnsureValidFacilitatorAsync(dataModel.FacilitatorId, studentDb.ChapterId);

        studentDb.FirstName = dataModel.StudentFirstName;
        studentDb.LastName = dataModel.StudentLastName;
        studentDb.NickName = dataModel.StudentNickName;
        studentDb.Email = dataModel.StudentEmail;
        studentDb.PhoneNumber = dataModel.StudentPhoneNumber;
        studentDb.Notes = dataModel.StudentNotes;
        studentDb.SecondarySchoolStartYear = dataModel.StudentSecondarySchoolStartYear;
        studentDb.UniversityStartYear = dataModel.StudentUniversityStartYear;
        studentDb.LastUpdatedById = dataModel.UpdatedById;
        studentDb.FacilitatorId = dataModel.FacilitatorId;

        db.Students.Update(studentDb);
        return await db.SaveChangesAsync();
    }

    private async Task EnsureValidFacilitatorAsync(int facilitatorId, int chapterId)
    {
        if (facilitatorId == 0)
        {
            throw new InvalidOperationException("El becario debe tener un mediador asignado.");
        }

        var facilitator = await userManager.FindByIdAsync(facilitatorId.ToString())
                          ?? throw new InvalidOperationException("El mediador seleccionado no existe.");

        var isLockedOut = facilitator.LockoutEnabled
                          && facilitator.LockoutEnd.HasValue
                          && facilitator.LockoutEnd.Value > DateTimeOffset.UtcNow;

        if (isLockedOut)
        {
            throw new InvalidOperationException("El mediador seleccionado está deshabilitado.");
        }

        if (facilitator.ChapterId != chapterId)
        {
            throw new InvalidOperationException("El mediador debe pertenecer a la filial del becario.");
        }

        if (!await userManager.IsInRoleAsync(facilitator, FonbecRole.Uploader))
        {
            throw new InvalidOperationException("El usuario seleccionado no es un mediador.");
        }
    }

    public async Task<string?> GetActiveStudentDisplayNameInChapterAsync(int studentId, int chapterId)
    {
        var student = await GetActiveStudentAsync(studentId, chapterId);
        return student?.DisplayName;
    }

    public async Task<ActiveStudentDataModel?> GetActiveStudentAsync(int studentId, int? chapterId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var student = await db.Students
            .AsNoTracking()
            .Where(s => s.Id == studentId
                        && s.IsActive
                        && !s.IsDeleted
                        && (chapterId == null || s.ChapterId == chapterId))
            .Select(s => new { s.FirstName, s.LastName, s.ChapterId })
            .FirstOrDefaultAsync();

        return student is null
            ? null
            : new ActiveStudentDataModel
            {
                DisplayName = $"{student.FirstName} {student.LastName}",
                ChapterId = student.ChapterId,
            };
    }

    public async Task<CandidateNameDataModel?> GetStudentNameAsync(int studentId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        return await db.Students
            .AsNoTracking()
            .Where(s => s.Id == studentId)
            .Select(s => new CandidateNameDataModel
            {
                Id = s.Id,
                FirstName = s.FirstName,
                LastName = s.LastName,
            })
            .FirstOrDefaultAsync();
    }

    public async Task<List<CandidateNameDataModel>> GetStudentCandidateNamesAsync(int excludeStudentId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        return await db.Students
            .AsNoTracking()
            .Where(s => s.Id != excludeStudentId && s.IsActive)
            .OrderBy(s => s.Id)
            .Select(s => new CandidateNameDataModel
            {
                Id = s.Id,
                FirstName = s.FirstName,
                LastName = s.LastName,
                IsCompany = false,
            })
            .ToListAsync();
    }

    public async Task<List<StudentImportMatchDataModel>> GetImportMatchKeysAsync(int chapterId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        return await db.Students
            .AsNoTracking()
            .Where(student => student.ChapterId == chapterId && !student.IsDeleted)
            .Select(student => new StudentImportMatchDataModel
            {
                Id = student.Id,
                FirstName = student.FirstName,
                LastName = student.LastName,
            })
            .ToListAsync();
    }
}