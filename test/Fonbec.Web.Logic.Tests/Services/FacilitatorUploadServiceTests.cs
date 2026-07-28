using FluentAssertions;
using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.DataModels.Facilitators;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models.Documents.Input;
using Fonbec.Web.Logic.Models.Facilitators.Input;
using Fonbec.Web.Logic.Models.Results;
using Fonbec.Web.Logic.Services;
using NSubstitute;

namespace Fonbec.Web.Logic.Tests.Services;

public class FacilitatorUploadServiceTests
{
    private readonly IFacilitatorRepository _facilitatorRepository = Substitute.For<IFacilitatorRepository>();
    private readonly IDocumentService _documentService = Substitute.For<IDocumentService>();
    private readonly ILetterExemptionService _letterExemptionService = Substitute.For<ILetterExemptionService>();
    private readonly FacilitatorUploadService _service;

    private const int FacilitatorId = 10;
    private const int StudentId = 5;
    private const int PlanId = 3;
    private const int SponsorId = 7;
    private const int CompanyId = 9;
    private const int ChapterId = 1;

    public FacilitatorUploadServiceTests()
    {
        _service = new FacilitatorUploadService(_facilitatorRepository, _documentService, _letterExemptionService, TimeProvider.System);
        _documentService.CreateLetterAsync(Arg.Any<CreateLetterInputModel>()).Returns(new CrudResult<long>(1));
        _documentService.CreateLetterWithBlobAsync(Arg.Any<CreateLetterWithBlobInputModel>()).Returns(new CrudResult<long>(1));
        _documentService.CreateReportCardAsync(Arg.Any<CreateReportCardInputModel>()).Returns(new CrudResult<long>(1));
        _documentService.CreateReportCardWithBlobAsync(Arg.Any<CreateReportCardWithBlobInputModel>()).Returns(new CrudResult<long>(1));
        _documentService.CreateOtherDocumentAsync(Arg.Any<CreateOtherDocumentInputModel>()).Returns(new CrudResult<long>(1));
        _documentService.CreateOtherDocumentWithBlobAsync(Arg.Any<CreateOtherDocumentWithBlobInputModel>()).Returns(new CrudResult<long>(1));
    }

    private static FacilitatorUploadContextDataModel StudentContext(
        DateTime? planStartsOn = null,
        string? sponsorFirstName = null,
        string? sponsorLastName = null,
        string? companyName = null,
        bool isActive = true,
        int facilitatorId = FacilitatorId) =>
        new()
        {
            StudentId = StudentId,
            StudentFirstName = "Juan",
            StudentLastName = "García",
            ChapterId = ChapterId,
            FacilitatorId = facilitatorId,
            IsActive = isActive,
            PlanStartsOn = planStartsOn,
            SponsorFirstName = sponsorFirstName,
            SponsorLastName = sponsorLastName,
            CompanyName = companyName,
        };

    // ---- GetUploadContextAsync ----

    [Fact]
    public async Task GetUploadContext_InvalidType_ReturnsNull()
    {
        _facilitatorRepository.GetUploadContextAsync(StudentId, null, null, null).Returns(StudentContext());

        var result = await _service.GetUploadContextAsync(FacilitatorId, StudentId, "invalid", null, null, null);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUploadContext_StudentNotAssignedToFacilitator_ReturnsNull()
    {
        _facilitatorRepository.GetUploadContextAsync(StudentId, null, null, null)
            .Returns(StudentContext(facilitatorId: 999));

        var result = await _service.GetUploadContextAsync(FacilitatorId, StudentId, "otro", null, null, null);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUploadContext_InactiveStudent_ReturnsNull()
    {
        _facilitatorRepository.GetUploadContextAsync(StudentId, null, null, null)
            .Returns(StudentContext(isActive: false));

        var result = await _service.GetUploadContextAsync(FacilitatorId, StudentId, "otro", null, null, null);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUploadContext_OtherDocument_ReturnsContext()
    {
        _facilitatorRepository.GetUploadContextAsync(StudentId, null, null, null).Returns(StudentContext());

        var result = await _service.GetUploadContextAsync(FacilitatorId, StudentId, "otro", null, null, null);

        result.Should().NotBeNull();
        result!.DocumentType.Should().Be(DocumentType.Other);
        result.StudentFullName.Should().Be("Juan García");
        result.AllowsTextContent.Should().BeTrue();
    }

    [Fact]
    public async Task GetUploadContext_LetterWithoutPlan_ReturnsNull()
    {
        _facilitatorRepository.GetUploadContextAsync(StudentId, null, SponsorId, null)
            .Returns(StudentContext(sponsorFirstName: "María", sponsorLastName: "López"));

        var result = await _service.GetUploadContextAsync(FacilitatorId, StudentId, "carta", SponsorId, null, null);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUploadContext_LetterWithBothSponsorAndCompany_ReturnsNull()
    {
        _facilitatorRepository.GetUploadContextAsync(StudentId, PlanId, SponsorId, CompanyId)
            .Returns(StudentContext(planStartsOn: new DateTime(2026, 3, 1),
                sponsorFirstName: "María", sponsorLastName: "López", companyName: "Acme"));

        var result = await _service.GetUploadContextAsync(FacilitatorId, StudentId, "carta", SponsorId, CompanyId, PlanId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUploadContext_LetterWithSponsor_ReturnsContextWithRecipientAndPeriod()
    {
        _facilitatorRepository.GetUploadContextAsync(StudentId, PlanId, SponsorId, null)
            .Returns(StudentContext(planStartsOn: new DateTime(2026, 3, 1),
                sponsorFirstName: "María", sponsorLastName: "López"));

        var result = await _service.GetUploadContextAsync(FacilitatorId, StudentId, "carta", SponsorId, null, PlanId);

        result.Should().NotBeNull();
        result!.DocumentType.Should().Be(DocumentType.Letter);
        result.RecipientName.Should().Be("María López");
        result.SponsorId.Should().Be(SponsorId);
        result.PlanId.Should().Be(PlanId);
        result.PlanPeriodLabel.Should().NotBeNullOrWhiteSpace();
        result.AllowsTextContent.Should().BeTrue();
    }

    [Fact]
    public async Task GetUploadContext_LetterWithCompany_ReturnsContextWithCompanyRecipient()
    {
        _facilitatorRepository.GetUploadContextAsync(StudentId, PlanId, null, CompanyId)
            .Returns(StudentContext(planStartsOn: new DateTime(2026, 3, 1), companyName: "Acme SA"));

        var result = await _service.GetUploadContextAsync(FacilitatorId, StudentId, "carta", null, CompanyId, PlanId);

        result.Should().NotBeNull();
        result!.RecipientName.Should().Be("Acme SA");
        result.CompanyId.Should().Be(CompanyId);
    }

    [Fact]
    public async Task GetUploadContext_LetterWhenStudentExempt_ReturnsNull()
    {
        _facilitatorRepository.GetUploadContextAsync(StudentId, PlanId, SponsorId, null)
            .Returns(StudentContext(planStartsOn: new DateTime(2026, 3, 1),
                sponsorFirstName: "María", sponsorLastName: "López"));
        _letterExemptionService.IsExemptAsync(StudentId, PlanId).Returns(true);

        var result = await _service.GetUploadContextAsync(FacilitatorId, StudentId, "carta", SponsorId, null, PlanId);

        result.Should().BeNull();
    }

    // ---- Upload delegation ----

    [Fact]
    public async Task UploadLetter_File_DelegatesToBlobCreate()
    {
        var input = new FacilitatorUploadLetterInputModel(
            StudentId, PlanId, SponsorId, null, UploadContentMode.File,
            [new UploadFileInputModel(new MemoryStream(new byte[8]), "application/pdf")], null, null, "una nota");

        var result = await _service.UploadLetterAsync(input, FacilitatorId);

        result.IsSuccess.Should().BeTrue();
        await _documentService.Received(1).CreateLetterWithBlobAsync(Arg.Is<CreateLetterWithBlobInputModel>(m =>
            m.StudentId == StudentId
            && m.PlanId == PlanId
            && m.SponsorId == SponsorId
            && m.Files.Count == 1
            && m.Files[0].MimeType == "application/pdf"
            && m.UploaderNotes == "una nota"
            && m.User.UserId == FacilitatorId
            && m.User.UserRole == FonbecRole.Uploader));
    }

    [Fact]
    public async Task UploadLetter_Text_DelegatesWithTextFileKind()
    {
        var input = new FacilitatorUploadLetterInputModel(
            StudentId, PlanId, SponsorId, null, UploadContentMode.Text,
            null, "Contenido de la carta", null, null);

        await _service.UploadLetterAsync(input, FacilitatorId);

        await _documentService.Received(1).CreateLetterAsync(Arg.Is<CreateLetterInputModel>(m =>
            m.FileKind == FileKind.Text
            && m.TextContent == "Contenido de la carta"
            && m.SponsorId == SponsorId));
    }

    [Fact]
    public async Task UploadLetter_YouTube_ParsesIdAndDelegates()
    {
        var input = new FacilitatorUploadLetterInputModel(
            StudentId, PlanId, SponsorId, null, UploadContentMode.YouTube,
            null, null, "https://youtu.be/dQw4w9WgXcQ", null);

        await _service.UploadLetterAsync(input, FacilitatorId);

        await _documentService.Received(1).CreateLetterAsync(Arg.Is<CreateLetterInputModel>(m =>
            m.FileKind == FileKind.YouTube
            && m.YouTubeVideoId == "dQw4w9WgXcQ"));
    }

    [Fact]
    public async Task UploadLetter_InvalidYouTube_ReturnsError()
    {
        var input = new FacilitatorUploadLetterInputModel(
            StudentId, PlanId, SponsorId, null, UploadContentMode.YouTube,
            null, null, "not a link", null);

        var result = await _service.UploadLetterAsync(input, FacilitatorId);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(DocumentMessages.YouTubeVideoIdRequired);
        await _documentService.DidNotReceive().CreateLetterAsync(Arg.Any<CreateLetterInputModel>());
    }

    [Fact]
    public async Task UploadLetter_WithoutRecipient_ReturnsError()
    {
        var input = new FacilitatorUploadLetterInputModel(
            StudentId, PlanId, null, null, UploadContentMode.Text,
            null, "texto", null, null);

        var result = await _service.UploadLetterAsync(input, FacilitatorId);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(DocumentMessages.LetterRequiresRecipient);
        await _documentService.DidNotReceive().CreateLetterAsync(Arg.Any<CreateLetterInputModel>());
    }

    [Fact]
    public async Task UploadLetter_WithBothRecipients_ReturnsError()
    {
        var input = new FacilitatorUploadLetterInputModel(
            StudentId, PlanId, SponsorId, CompanyId, UploadContentMode.Text,
            null, "texto", null, null);

        var result = await _service.UploadLetterAsync(input, FacilitatorId);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(DocumentMessages.LetterRequiresRecipient);
        await _documentService.DidNotReceive().CreateLetterAsync(Arg.Any<CreateLetterInputModel>());
    }

    [Fact]
    public async Task UploadLetter_Company_Text_DelegatesWithCompanyId()
    {
        var input = new FacilitatorUploadLetterInputModel(
            StudentId, PlanId, null, CompanyId, UploadContentMode.Text,
            null, "Contenido de la carta", null, null);

        var result = await _service.UploadLetterAsync(input, FacilitatorId);

        result.IsSuccess.Should().BeTrue();
        await _documentService.Received(1).CreateLetterAsync(Arg.Is<CreateLetterInputModel>(m =>
            m.FileKind == FileKind.Text
            && m.TextContent == "Contenido de la carta"
            && m.SponsorId == null
            && m.CompanyId == CompanyId));
    }

    [Fact]
    public async Task UploadLetter_Company_File_DelegatesWithCompanyId()
    {
        var input = new FacilitatorUploadLetterInputModel(
            StudentId, PlanId, null, CompanyId, UploadContentMode.File,
            [new UploadFileInputModel(new MemoryStream(new byte[8]), "application/pdf")], null, null, null);

        var result = await _service.UploadLetterAsync(input, FacilitatorId);

        result.IsSuccess.Should().BeTrue();
        await _documentService.Received(1).CreateLetterWithBlobAsync(Arg.Is<CreateLetterWithBlobInputModel>(m =>
            m.SponsorId == null
            && m.CompanyId == CompanyId
            && m.Files[0].MimeType == "application/pdf"));
    }

    [Fact]
    public async Task UploadLetter_WhenStudentExempt_ReturnsErrorWithoutDelegating()
    {
        _letterExemptionService.IsExemptAsync(StudentId, PlanId).Returns(true);
        var input = new FacilitatorUploadLetterInputModel(
            StudentId, PlanId, SponsorId, null, UploadContentMode.Text,
            null, "Contenido de la carta", null, null);

        var result = await _service.UploadLetterAsync(input, FacilitatorId);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(DocumentMessages.LetterExemptForPlan);
        await _documentService.DidNotReceive().CreateLetterAsync(Arg.Any<CreateLetterInputModel>());
    }

    [Fact]
    public async Task UploadReportCard_Text_ReturnsErrorWithoutDelegating()
    {
        var input = new FacilitatorUploadReportCardInputModel(
            StudentId, new DateOnly(2026, 3, 1), "Boletín 1º trimestre", UploadContentMode.Text,
            null, null, null);

        var result = await _service.UploadReportCardAsync(input, FacilitatorId);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(DocumentMessages.ReportCardCannotUseText);
        await _documentService.DidNotReceive().CreateReportCardAsync(Arg.Any<CreateReportCardInputModel>());
    }

    [Fact]
    public async Task UploadReportCard_File_DelegatesToBlobCreate()
    {
        var input = new FacilitatorUploadReportCardInputModel(
            StudentId, new DateOnly(2026, 3, 1), "Boletín 1º trimestre", UploadContentMode.File,
            [new UploadFileInputModel(new MemoryStream(new byte[8]), "image/jpeg")], null, null);

        await _service.UploadReportCardAsync(input, FacilitatorId);

        await _documentService.Received(1).CreateReportCardWithBlobAsync(Arg.Is<CreateReportCardWithBlobInputModel>(m =>
            m.StudentId == StudentId
            && m.Description == "Boletín 1º trimestre"
            && m.Period == new DateOnly(2026, 3, 1)
            && m.Files[0].MimeType == "image/jpeg"));
    }

    [Fact]
    public async Task UploadOther_Text_DelegatesWithTextFileKind()
    {
        var input = new FacilitatorUploadOtherInputModel(
            StudentId, "Certificado de alumno regular", UploadContentMode.Text,
            null, "un texto", null, null);

        await _service.UploadOtherDocumentAsync(input, FacilitatorId);

        await _documentService.Received(1).CreateOtherDocumentAsync(Arg.Is<CreateOtherDocumentInputModel>(m =>
            m.FileKind == FileKind.Text
            && m.Description == "Certificado de alumno regular"
            && m.TextContent == "un texto"));
    }

    [Fact]
    public async Task UploadOther_File_DelegatesToBlobCreate()
    {
        var input = new FacilitatorUploadOtherInputModel(
            StudentId, "Constancia", UploadContentMode.File,
            [new UploadFileInputModel(new MemoryStream(new byte[8]), "text/plain")], null, null, null);

        await _service.UploadOtherDocumentAsync(input, FacilitatorId);

        await _documentService.Received(1).CreateOtherDocumentWithBlobAsync(Arg.Is<CreateOtherDocumentWithBlobInputModel>(m =>
            m.StudentId == StudentId
            && m.Description == "Constancia"
            && m.Files[0].MimeType == "text/plain"));
    }
}