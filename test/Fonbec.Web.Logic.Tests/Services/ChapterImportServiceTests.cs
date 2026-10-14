using System.Text;
using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Import;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Import;
using Fonbec.Web.Logic.Models.Results;
using Fonbec.Web.Logic.Models.Sponsors.Input;
using Fonbec.Web.Logic.Models.Students.Input;
using Fonbec.Web.Logic.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Fonbec.Web.Logic.Tests.Services;

public class ChapterImportServiceTests
{
    private const int ChapterId = 4;
    private const int CreatedById = 2;
    private const int DefaultFacilitatorId = 7;

    private readonly IStudentRepository _students = Substitute.For<IStudentRepository>();
    private readonly ISponsorRepository _sponsors = Substitute.For<ISponsorRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IStudentService _studentService = Substitute.For<IStudentService>();
    private readonly ISponsorService _sponsorService = Substitute.For<ISponsorService>();
    private readonly ChapterImportService _service;

    public ChapterImportServiceTests()
    {
        _students.GetImportMatchKeysAsync(ChapterId).Returns(
        [
            new StudentImportMatchDataModel { Id = 1, FirstName = "Ana", LastName = "Pérez" },
        ]);
        _sponsors.GetImportEmailsAsync(ChapterId).Returns(["ana@ejemplo.com"]);
        _users.GetActiveFacilitatorsAsync(ChapterId).Returns(
        [
            new FacilitatorImportDataModel { Id = DefaultFacilitatorId, Email = "mediador@filial.com" },
            new FacilitatorImportDataModel { Id = 9, Email = "otro@filial.com" },
        ]);
        _studentService.CreateStudentAsync(Arg.Any<CreateStudentInputModel>()).Returns(new CrudResult(1));
        _sponsorService.CreateSponsorAsync(Arg.Any<CreateSponsorInputModel>()).Returns(new CrudResult(1));
        _service = new ChapterImportService(_students, _sponsors, _users, _studentService, _sponsorService);
    }

    [Fact]
    public async Task Preview_Skips_Existing_People_And_Duplicates_In_The_File()
    {
        var result = await _service.PreviewAsync(Request(Students(
            "Luis;Gómez",
            "luis;gómez",
            "  ana  ;pérez")));

        result.FileErrors.Should().BeEmpty();
        result.Rows.Should().HaveCount(3);
        result.Rows[0].Status.Should().Be(ChapterImportRowStatus.Create);
        result.Rows[1].Status.Should().Be(ChapterImportRowStatus.Skip);
        result.Rows[1].Detail.Should().Be("Ya está en el archivo.");
        result.Rows[2].Status.Should().Be(ChapterImportRowStatus.Skip);
        result.Rows[2].Detail.Should().Be("Ya existe en la filial.");
        await _studentService.DidNotReceive().CreateStudentAsync(Arg.Any<CreateStudentInputModel>());
    }

    [Fact]
    public async Task Preview_Skips_Existing_Sponsor_By_Email()
    {
        var result = await _service.PreviewAsync(Request(Sponsors(
            "Ana;Pérez;ANA@ejemplo.com",
            "Luis;Díaz;Luis@Ejemplo.com",
            "Marta;Sosa;luis@ejemplo.com")));

        result.Rows.Select(row => row.Status).Should().Equal(
            ChapterImportRowStatus.Skip,
            ChapterImportRowStatus.Create,
            ChapterImportRowStatus.Skip);
        result.Rows[0].Detail.Should().Be("Ya existe en la filial.");
        result.Rows[2].Detail.Should().Be("Ya está en el archivo.");
        await _sponsorService.DidNotReceive().CreateSponsorAsync(Arg.Any<CreateSponsorInputModel>());
    }

    [Fact]
    public async Task Preview_Uses_Facilitator_Email_Instead_Of_The_Default()
    {
        var csv = "Nombre;Apellido;Mediador\nLuis;Gómez;Otro@Filial.com\n";
        var result = await _service.CommitAsync(Request(StudentsRaw(csv)));

        result.Rows.Should().ContainSingle();
        result.Rows[0].Status.Should().Be(ChapterImportRowStatus.Created);
        await _studentService.Received(1).CreateStudentAsync(Arg.Is<CreateStudentInputModel>(model =>
            model.FacilitatorId == 9
            && model.ChapterId == ChapterId
            && model.CreatedById == CreatedById
            && model.StudentGender == Gender.Male));
    }

    [Fact]
    public async Task Unknown_Facilitator_And_Missing_Default_Are_Errors_And_Do_Not_Create()
    {
        var unknown = await _service.PreviewAsync(Request(StudentsRaw(
            "Nombre;Apellido;Mediador\nLuis;Gómez;nadie@filial.com\n")));
        var missingDefault = await _service.PreviewAsync(Request(
            StudentsRaw("Nombre;Apellido\nLuis;Gómez\n"),
            defaultFacilitatorId: null));

        unknown.Rows.Single().Status.Should().Be(ChapterImportRowStatus.Error);
        unknown.Rows.Single().Detail.Should().Contain("mediador");
        missingDefault.Rows.Single().Status.Should().Be(ChapterImportRowStatus.Error);
        await _studentService.DidNotReceive().CreateStudentAsync(Arg.Any<CreateStudentInputModel>());
    }

    [Fact]
    public async Task Commit_Creates_Only_New_Valid_Rows_And_Continues_After_A_Failure()
    {
        _studentService.CreateStudentAsync(Arg.Is<CreateStudentInputModel>(model => model.StudentFirstName == "Luis"))
            .ThrowsAsync(new InvalidOperationException("El mediador seleccionado no existe."));

        var result = await _service.CommitAsync(Request(Students(
            "Luis;Gómez",
            "Marta;Sosa",
            "ana;pérez")));

        result.Rows[0].Status.Should().Be(ChapterImportRowStatus.Failed);
        result.Rows[0].Detail.Should().Contain("mediador");
        result.Rows[1].Status.Should().Be(ChapterImportRowStatus.Created);
        result.Rows[2].Status.Should().Be(ChapterImportRowStatus.Skip);
        await _studentService.Received(1).CreateStudentAsync(Arg.Is<CreateStudentInputModel>(model =>
            model.StudentFirstName == "Marta"));
        await _studentService.DidNotReceive().CreateStudentAsync(Arg.Is<CreateStudentInputModel>(model =>
            model.StudentFirstName == "ana" || model.StudentFirstName == "Ana"));
    }

    [Fact]
    public async Task Commit_Stores_School_Years_And_Gender()
    {
        var csv = "Nombre;Apellido;Género;Inicio secundaria;Inicio universidad\nLuis;Gómez;Femenino;2020;2024\n";
        await _service.CommitAsync(Request(StudentsRaw(csv)));

        await _studentService.Received(1).CreateStudentAsync(Arg.Is<CreateStudentInputModel>(model =>
            model.StudentGender == Gender.Female
            && model.StudentSecondarySchoolStartYear == new DateTime(2020, 1, 1)
            && model.StudentUniversityStartYear == new DateTime(2024, 1, 1)
            && model.FacilitatorId == DefaultFacilitatorId));
    }

    [Fact]
    public async Task Invalid_Gender_And_School_Order_Are_Row_Errors()
    {
        var csv = "Nombre;Apellido;Género;Inicio secundaria;Inicio universidad\nLuis;Gómez;Otro;2024;2020\n";
        var result = await _service.PreviewAsync(Request(StudentsRaw(csv)));

        result.Rows.Single().Status.Should().Be(ChapterImportRowStatus.Error);
        result.Rows.Single().Detail.Should().Contain("Género");
        result.Rows.Single().Detail.Should().Contain("universidad");
    }

    [Fact]
    public async Task Another_Chapter_Is_Rejected()
    {
        var request = Request(Students("Luis;Gómez")) with { CallerChapterId = ChapterId + 1 };

        var result = await _service.PreviewAsync(request);

        result.FileErrors.Should().ContainSingle();
        await _students.DidNotReceive().GetImportMatchKeysAsync(Arg.Any<int>());
        await _studentService.DidNotReceive().CreateStudentAsync(Arg.Any<CreateStudentInputModel>());
    }

    private ChapterImportRequest Request(
        ChapterImportUpload file,
        int? defaultFacilitatorId = DefaultFacilitatorId) =>
        new(ChapterId, CallerChapterId: null, CreatedById, defaultFacilitatorId, [file]);

    private static ChapterImportUpload Students(params string[] rows) =>
        StudentsRaw("Nombre;Apellido\n" + string.Join('\n', rows) + "\n");

    private static ChapterImportUpload StudentsRaw(string csv) =>
        new("becarios.csv", Encoding.UTF8.GetBytes(csv), ChapterImportUploadKind.StudentsCsv);

    private static ChapterImportUpload Sponsors(params string[] rows) =>
        new(
            "padrinos.csv",
            Encoding.UTF8.GetBytes("Nombre;Apellido;Email\n" + string.Join('\n', rows) + "\n"),
            ChapterImportUploadKind.SponsorsCsv);
}