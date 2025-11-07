using Fonbec.Web.DataAccess.DataModels.Students;
using Fonbec.Web.DataAccess.DataModels.Students.Input;
using Fonbec.Web.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Repositories;

public interface IStudentRepository
{
    Task<List<AllStudentsDataModel>> GetAllStudentsAsync();
    Task<int> CreateStudentAsync(CreateStudentInputDataModel inputDataModel);
    Task<int> UpdateStudentAsync(UpdateStudentInputDataModel dataModel);
}

public class StudentRepository(IDbContextFactory<FonbecWebDbContext> dbContext) : IStudentRepository
{
    public async Task<List<AllStudentsDataModel>> GetAllStudentsAsync()
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var allStudents = await db.Students
            .Include(s => s.Facilitator)
            .Include(s => s.CreatedBy)
            .Include(s => s.LastUpdatedBy)
            .Include(s => s.DisabledBy)
            .Include(s => s.ReenabledBy)
            .Where(s => s.IsActive)
            .Select(s => new AllStudentsDataModel(s)
            {
                StudentId = s.Id,
                StudentFirstName = s.FirstName,
                StudentLastName = s.LastName,
                StundentNickName = s.NickName,
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
                StudentPhoneNumber = s.PhoneNumber
            })
            .OrderBy(s => s.StudentFirstName)
            .ThenBy(s => s.StudentLastName)
            .ToListAsync();

        return allStudents;
    }

    public async Task<int> CreateStudentAsync(CreateStudentInputDataModel inputDataModel)
    {
        await using var db = await dbContext.CreateDbContextAsync();

        var student = new Student
        {
            StudentId = inputDataModel.StudentId,
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
}