using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Students;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Abstract;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Services;
using Mapster;
using NSubstitute;

namespace Fonbec.Web.Logic.Tests.Services;

public class StudentServiceTests
{
    private const int StudentId = 10;
    private const int ChapterId = 1;

    private readonly IStudentRepository _studentRepository = Substitute.For<IStudentRepository>();
    private readonly StudentService _service;

    public StudentServiceTests()
    {
        TypeAdapterConfig.GlobalSettings.Scan(typeof(StudentService).Assembly);
        _service = new StudentService(_studentRepository);
    }

    [Fact]
    public async Task GetActiveStudentDisplayNameInChapterAsync_Returns_Name_When_Student_Is_In_Chapter()
    {
        _studentRepository.GetActiveStudentDisplayNameInChapterAsync(StudentId, ChapterId)
            .Returns("Ana Pérez");

        var result = await _service.GetActiveStudentDisplayNameInChapterAsync(StudentId, ChapterId);

        result.Should().Be("Ana Pérez");
        await _studentRepository.Received(1).GetActiveStudentDisplayNameInChapterAsync(StudentId, ChapterId);
    }

    [Fact]
    public async Task GetActiveStudentDisplayNameInChapterAsync_Returns_Null_When_Student_Is_Unknown()
    {
        _studentRepository.GetActiveStudentDisplayNameInChapterAsync(99, ChapterId)
            .Returns((string?)null);

        var result = await _service.GetActiveStudentDisplayNameInChapterAsync(99, ChapterId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetActiveStudentDisplayNameInChapterAsync_Returns_Null_When_Student_Is_In_Another_Chapter()
    {
        _studentRepository.GetActiveStudentDisplayNameInChapterAsync(StudentId, ChapterId)
            .Returns((string?)null);

        var result = await _service.GetActiveStudentDisplayNameInChapterAsync(StudentId, ChapterId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllStudentsAsync_Attaches_Letter_Assessment()
    {
        _studentRepository.GetAllStudentsAsync(ChapterId).Returns([Student()]);
        _studentRepository.GetApprovedLetterScoresAsync(ChapterId).Returns(
        [
            new StudentLetterScoreDataModel
            {
                StudentId = StudentId,
                PlanId = 1,
                PlanStartsOn = new DateTime(2026, 3, 1),
                SpellingScore = 4,
                PenmanshipScore = 4,
                ContentScore = 4,
            },
        ]);

        var students = await _service.GetAllStudentsAsync(ChapterId);

        students.Should().ContainSingle(student =>
            student.LetterAssessment != null
            && student.LetterAssessment.OverallText == "4,0"
            && !student.LetterAssessment.ShowTrend);
    }

    [Fact]
    public async Task GetAllStudentsAsync_Leaves_Assessment_Empty_When_There_Are_No_Approved_Letters()
    {
        _studentRepository.GetAllStudentsAsync(ChapterId).Returns([Student()]);
        _studentRepository.GetApprovedLetterScoresAsync(ChapterId).Returns([]);

        var students = await _service.GetAllStudentsAsync(ChapterId);

        students.Single().LetterAssessment.Should().BeNull();
    }

    private static AllStudentsDataModel Student()
    {
        var auditable = Substitute.For<Auditable>();
        auditable.CreatedBy = new FonbecWebUser { FirstName = "Ana", LastName = "Pérez" };
        auditable.CreatedOnUtc = new DateTime(2026, 1, 1);

        return new AllStudentsDataModel(auditable)
        {
            StudentId = StudentId,
            StudentFirstName = "Juan",
            StudentLastName = "García",
            FacilitatorFirstName = "Ana",
            FacilitatorLastName = "Pérez",
            StudentChapterName = "Córdoba",
        };
    }
}