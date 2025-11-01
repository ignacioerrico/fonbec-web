using FluentAssertions;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Fonbec.Web.DataAccess.Tests.Repositories;

public class StudentRepositoryTests
{
    private static FonbecWebDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<FonbecWebDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;

        return new FonbecWebDbContext(options);
    }

    private static IStudentRepository CreateRepositoryWithSeedData(out List<Student> seededStudents)
    {
        var dbContext = CreateInMemoryContext();

        // Seed facilitator users
        var facilitator1 = new FonbecWebUser
        {
            Id = 10,
            FirstName = "FacilitatorA",
            LastName = "Alpha",
            Email = "facA@example.com"
        };

        var facilitator2 = new FonbecWebUser
        {
            Id = 11,
            FirstName = "FacilitatorB",
            LastName = "Beta",
            Email = "facB@example.com"
        };

        // Seed audit users
        var createdBy = new FonbecWebUser
        {
            Id = 20,
            FirstName = "Creator",
            LastName = "User",
            Email = "creator@example.com"
        };

        dbContext.Users.AddRange(facilitator1, facilitator2, createdBy);
        dbContext.SaveChanges();

        // Active students
        var student1 = new Student
        {
            Id = 1,
            FirstName = "Anna",
            LastName = "Zephyr",
            NickName = "Ann",
            Gender = Gender.Female,
            Email = "anna@example.com",
            PhoneNumber = "111-111",
            Notes = "Notes 1",
            SecondarySchoolStartYear = new DateTime(2022, 1, 1),
            FacilitatorId = facilitator1.Id,
            Facilitator = facilitator1,
            CreatedById = createdBy.Id,
            CreatedBy = createdBy,
            IsActive = true
        };

        var student2 = new Student
        {
            Id = 2,
            FirstName = "Brian",
            LastName = "Young",
            NickName = null,
            Gender = Gender.Male,
            Email = "brian@example.com",
            PhoneNumber = "222-222",
            Notes = "Notes 2",
            UniversityStartYear = new DateTime(2024, 1, 1),
            FacilitatorId = facilitator2.Id,
            Facilitator = facilitator2,
            CreatedById = createdBy.Id,
            CreatedBy = createdBy,
            IsActive = true
        };

        // Inactive student (must be filtered out)
        var student3 = new Student
        {
            Id = 3,
            FirstName = "Charlie",
            LastName = "Xavier",
            Gender = Gender.Male,
            FacilitatorId = facilitator1.Id,
            Facilitator = facilitator1,
            CreatedById = createdBy.Id,
            CreatedBy = createdBy,
            IsActive = false
        };

        dbContext.Students.AddRange(student1, student2, student3);
        dbContext.SaveChanges();

        // Mark student3 as inactive (must be done AFTER initial Add because Added event forces IsActive = true)
        student3.DisabledById = createdBy.Id; // any non-null value
        dbContext.Students.Update(student3);
        dbContext.SaveChanges();

        seededStudents = [student1, student2, student3];

        var factory = Substitute.For<IDbContextFactory<FonbecWebDbContext>>();
        factory.CreateDbContextAsync().Returns(Task.FromResult(dbContext));

        return new StudentRepository(factory);
    }

    [Fact]
    public async Task GetAllStudentsAsync_Returns_OnlyActive_Projected_AndSorted()
    {
        // Arrange
        var repository = CreateRepositoryWithSeedData(out var seededStudents);

        var expectedActive = seededStudents.Where(s => s.IsActive).ToList();

        // Act
        var result = await repository.GetAllStudentsAsync();

        // Assert
        result.Should().HaveCount(2);

        // Sorted by FirstName then LastName: Anna Zephyr, Brian Young
        result[0].StudentFirstName.Should().Be("Anna");
        result[0].StudentLastName.Should().Be("Zephyr");
        result[1].StudentFirstName.Should().Be("Brian");
        result[1].StudentLastName.Should().Be("Young");

        // Verify inactive student excluded
        result.Any(r => r.StudentFirstName == "Charlie").Should().BeFalse();

        // Projection checks for first student
        var anna = result[0];
        var annaSource = expectedActive.First(s => s.FirstName == "Anna");
        anna.StudentId.Should().Be(annaSource.Id);
        anna.StundentNickName.Should().Be(annaSource.NickName);
        anna.StudentGender.Should().Be(annaSource.Gender);
        anna.IsStudentActive.Should().BeTrue();
        anna.FacilitatorId.Should().Be(annaSource.FacilitatorId);
        anna.FacilitatorFirstName.Should().Be(annaSource.Facilitator.FirstName);
        anna.FacilitatorLastName.Should().Be(annaSource.Facilitator.LastName);
        anna.FacilitatorEmail.Should().Be(annaSource.Facilitator.Email);
        anna.StudentEmail.Should().Be(annaSource.Email);
        anna.Notes.Should().Be(annaSource.Notes);
        anna.StudentSecondarySchoolStartYear.Should().Be(annaSource.SecondarySchoolStartYear);
        anna.StudentUniversityStartYear.Should().Be(annaSource.UniversityStartYear);
        anna.StudentPhoneNumber.Should().Be(annaSource.PhoneNumber);

        // Education level mapping (basic sanity: property copied)
        anna.StudentCurrentEducationLevel.Should().Be(annaSource.CurrentEducationLevel);
    }

    [Fact]
    public async Task GetAllStudentsAsync_ReturnsEmpty_WhenNoActiveStudents()
    {
        // Arrange
        var dbContext = CreateInMemoryContext();

        var activeStudents = dbContext.Students.Where(s => new[] { 1, 2 }.Contains(s.Id));
        dbContext.Students.RemoveRange(activeStudents);

        await dbContext.SaveChangesAsync();

        var factory = Substitute.For<IDbContextFactory<FonbecWebDbContext>>();
        factory.CreateDbContextAsync().Returns(Task.FromResult(dbContext));

        var repository = new StudentRepository(factory);

        // Act
        var result = await repository.GetAllStudentsAsync();

        // Assert
        result.Should().BeEmpty();
    }
}