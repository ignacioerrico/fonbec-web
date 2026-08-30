using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.DataModels.Review;
using Fonbec.Web.DataAccess.DataModels.Students;
using Fonbec.Web.DataAccess.DataModels.Students.Input;
using Fonbec.Web.DataAccess.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Repositories;

public interface IStudentRepository
{
    Task<List<AllStudentsDataModel>> GetAllStudentsAsync(int? chapterId);
    Task<int?> GetStudentChapterIdAsync(int studentId);
    Task<int> CreateStudentAsync(CreateStudentInputDataModel inputDataModel);
    Task<int> UpdateStudentAsync(UpdateStudentInputDataModel dataModel);

    /// <summary>Get a single student's name, or <c>null</c> when the student does not exist.</summary>
    Task<CandidateNameDataModel?> GetStudentNameAsync(int studentId);

    /// <summary>
    /// Get up to <paramref name="count"/> random student names, excluding <paramref name="excludeStudentId"/>.
    /// Sampling is performed server-side (SQL Server <c>NEWID()</c>) and is not chapter-restricted.
    /// </summary>
    Task<List<CandidateNameDataModel>> GetRandomStudentNamesAsync(int excludeStudentId, int count);
}

public class StudentRepository(IDbContextFactory<FonbecWebDbContext> dbContext,
    UserManager<FonbecWebUser> userManager) : IStudentRepository
{
    public async Task<List<AllStudentsDataModel>> GetAllStudentsAsync(int? chapterId)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var utcNow = DateTime.UtcNow;

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
                                 && sp.StartDate <= utcNow
                                 && (sp.EndDate == null || sp.EndDate >= utcNow)
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
                    })
                    .ToList(),
            })
            .OrderBy(s => s.StudentFirstName)
            .ThenBy(s => s.StudentLastName)
            .ToListAsync();

        return allStudents;
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

        // Defense-in-depth: validate facilitator belongs to the student's chapter and is an Uploader
        var facilitator = await userManager.FindByIdAsync(inputDataModel.FacilitatorId.ToString());
        if (facilitator is null)
        {
            throw new InvalidOperationException("El mediador seleccionado no existe.");
        }
        if (facilitator.ChapterId != inputDataModel.ChapterId)
        {
            throw new InvalidOperationException("El mediador debe pertenecer a la filial del becario.");
        }
        var isInRole = await userManager.IsInRoleAsync(facilitator, FonbecRole.Uploader);
        if (!isInRole)
        {
            throw new InvalidOperationException("El usuario seleccionado no es un mediador.");
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

        studentDb.FirstName = dataModel.StudentFirstName;
        studentDb.LastName = dataModel.StudentLastName;
        studentDb.NickName = dataModel.StudentNickName;
        studentDb.Email = dataModel.StudentEmail;
        studentDb.PhoneNumber = dataModel.StudentPhoneNumber;
        studentDb.Notes = dataModel.StudentNotes;
        studentDb.SecondarySchoolStartYear = dataModel.StudentSecondarySchoolStartYear;
        studentDb.UniversityStartYear = dataModel.StudentUniversityStartYear;
        studentDb.FacilitatorId = dataModel.FacilitatorId;
        studentDb.LastUpdatedById = dataModel.UpdatedById;

        db.Students.Update(studentDb);
        return await db.SaveChangesAsync();
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

    public async Task<List<CandidateNameDataModel>> GetRandomStudentNamesAsync(int excludeStudentId, int count)
    {
        if (count <= 0)
        {
            return [];
        }

        await using var db = await dbContext.CreateDbContextAsync();

        return await db.Students
            .AsNoTracking()
            .Where(s => s.Id != excludeStudentId && s.IsActive)
            .OrderBy(_ => Guid.NewGuid())
            .Take(count)
            .Select(s => new CandidateNameDataModel
            {
                Id = s.Id,
                FirstName = s.FirstName,
                LastName = s.LastName,
            })
            .ToListAsync();
    }
}