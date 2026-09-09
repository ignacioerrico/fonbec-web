using FluentAssertions;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Services;
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
}