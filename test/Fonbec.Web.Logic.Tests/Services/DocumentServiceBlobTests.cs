using System.Text;
using FluentAssertions;
using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.DataModels.Documents;
using Fonbec.Web.DataAccess.DataModels.Documents.Input;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models.Documents;
using Fonbec.Web.Logic.Models.Documents.Input;
using Fonbec.Web.Logic.Options;
using Fonbec.Web.Logic.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Fonbec.Web.Logic.Tests.Services;

public class DocumentServiceBlobTests
{
    private readonly IDocumentRepository _repository = Substitute.For<IDocumentRepository>();
    private readonly IDocumentNotificationService _notificationService = Substitute.For<IDocumentNotificationService>();
    private readonly IUserService _userService = Substitute.For<IUserService>();
    private readonly IBlobStorageService _blobStorageService = Substitute.For<IBlobStorageService>();
    private readonly ILetterPlanProgressService _letterPlanProgressService = Substitute.For<ILetterPlanProgressService>();
    private readonly IPlanCompletionService _planCompletionService = Substitute.For<IPlanCompletionService>();

    private const int UploaderId = 10;
    private const int StudentId = 5;
    private const int PlanId = 3;
    private const int SponsorId = 7;
    private const int CompanyId = 9;
    private const int ChapterId = 1;

    private DocumentService CreateService(BlobStorageOptions? options = null) =>
        new(_repository,
            _notificationService,
            _userService,
            _blobStorageService,
            _letterPlanProgressService,
            _planCompletionService,
            Microsoft.Extensions.Options.Options.Create(options ?? new BlobStorageOptions()),
            NullLogger<DocumentService>.Instance);

    private void ConfigureValidLetterUpload()
    {
        _repository.GetStudentUploadContextAsync(StudentId).Returns(new StudentUploadContextDataModel
        {
            StudentId = StudentId,
            ChapterId = ChapterId,
            FacilitatorId = UploaderId,
            IsActive = true,
        });
        _repository.IsActivePlanAsync(PlanId, ChapterId).Returns(true);
        _repository.HasActiveSponsorshipAsync(StudentId, SponsorId).Returns(true);
        _repository.HasDuplicateLetterAsync(StudentId, SponsorId, PlanId).Returns(false);

        _blobStorageService
            .UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => new UploadBlobResult
            {
                BlobName = callInfo.ArgAt<string>(1),
                MimeType = callInfo.ArgAt<string>(2),
                FileSizeBytes = 10,
                Sha256 = [1, 2, 3],
            });
    }

    private static CreateLetterWithBlobInputModel LetterInput(string mimeType, int contentLength = 10) =>
        new(StudentId, PlanId, SponsorId,
            new CreateDocumentUserContext(UploaderId, "Uploader", ChapterId, null),
            [new UploadFileInputModel(new MemoryStream(new byte[contentLength]), mimeType)]);

    private void ConfigureValidCompanyLetterUpload()
    {
        _repository.GetStudentUploadContextAsync(StudentId).Returns(new StudentUploadContextDataModel
        {
            StudentId = StudentId,
            ChapterId = ChapterId,
            FacilitatorId = UploaderId,
            IsActive = true,
        });
        _repository.IsActivePlanAsync(PlanId, ChapterId).Returns(true);
        _repository.HasActiveCompanySponsorshipAsync(StudentId, CompanyId).Returns(true);
        _repository.HasDuplicateCompanyLetterAsync(StudentId, CompanyId, PlanId).Returns(false);
        _repository.CreateLetterAsync(Arg.Any<CreateLetterInputDataModel>())
            .Returns(new CreateDocumentResultDataModel { DocumentId = 1 });

        _blobStorageService
            .UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => new UploadBlobResult
            {
                BlobName = callInfo.ArgAt<string>(1),
                MimeType = callInfo.ArgAt<string>(2),
                FileSizeBytes = 10,
                Sha256 = [1, 2, 3],
            });
    }

    [Fact]
    public async Task CompanyLetter_ValidatesCompanyRecipient_UploadsWithCompanyPathAndId()
    {
        ConfigureValidCompanyLetterUpload();
        var service = CreateService();

        var input = new CreateLetterWithBlobInputModel(
            StudentId, PlanId, null,
            new CreateDocumentUserContext(UploaderId, "Uploader", ChapterId, null),
            [new UploadFileInputModel(new MemoryStream(new byte[10]), "application/pdf")], CompanyId: CompanyId);

        var result = await service.CreateLetterWithBlobAsync(input);

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).HasActiveCompanySponsorshipAsync(StudentId, CompanyId);
        await _repository.DidNotReceive().HasActiveSponsorshipAsync(Arg.Any<int>(), Arg.Any<int>());
        await _repository.Received(1).CreateLetterAsync(Arg.Is<CreateLetterInputDataModel>(m =>
            m.SponsorId == null && m.CompanyId == CompanyId));
        await _blobStorageService.Received(1).UploadAsync(
            Arg.Any<Stream>(),
            Arg.Is<string>(name => name.Contains($"/company-{CompanyId}/")),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PersonLetter_UploadsWithSponsorPathAndId()
    {
        ConfigureValidLetterUpload();
        _repository.CreateLetterAsync(Arg.Any<CreateLetterInputDataModel>())
            .Returns(new CreateDocumentResultDataModel { DocumentId = 1 });
        var service = CreateService();

        var result = await service.CreateLetterWithBlobAsync(LetterInput("application/pdf"));

        result.IsSuccess.Should().BeTrue();
        await _repository.Received(1).CreateLetterAsync(Arg.Is<CreateLetterInputDataModel>(m =>
            m.SponsorId == SponsorId && m.CompanyId == null));
        await _blobStorageService.Received(1).UploadAsync(
            Arg.Any<Stream>(),
            Arg.Is<string>(name => name.Contains($"/sponsor-{SponsorId}/")),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Letter_WithBothRecipients_ReturnsRecipientErrorAndDoesNotUpload()
    {
        ConfigureValidLetterUpload();
        var service = CreateService();

        var input = new CreateLetterWithBlobInputModel(
            StudentId, PlanId, SponsorId,
            new CreateDocumentUserContext(UploaderId, "Uploader", ChapterId, null),
            [new UploadFileInputModel(new MemoryStream(new byte[10]), "application/pdf")], CompanyId: CompanyId);

        var result = await service.CreateLetterWithBlobAsync(input);

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(DocumentMessages.LetterRequiresRecipient);
        await _blobStorageService.DidNotReceive().UploadAsync(
            Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Scenario07_DisallowedMimeType_ReturnsErrorAndDoesNotUpload()
    {
        ConfigureValidLetterUpload();
        var service = CreateService();

        var result = await service.CreateLetterWithBlobAsync(LetterInput("application/msword"));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(DocumentMessages.InvalidMimeType);
        await _blobStorageService.DidNotReceive().UploadAsync(
            Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Scenario08_FileExceedsMaxSize_ReturnsErrorAndDoesNotUpload()
    {
        ConfigureValidLetterUpload();
        var service = CreateService(new BlobStorageOptions { MaxFileSizeBytes = 4 });

        var result = await service.CreateLetterWithBlobAsync(LetterInput("application/pdf", contentLength: 100));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(DocumentMessages.TotalFileSizeTooLarge);
        await _blobStorageService.DidNotReceive().UploadAsync(
            Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Scenario09_DatabaseInsertFails_DeletesUploadedBlob()
    {
        ConfigureValidLetterUpload();
        _repository.CreateLetterAsync(Arg.Any<CreateLetterInputDataModel>())
            .Returns(new CreateDocumentResultDataModel { Errors = [DocumentMessages.ConcurrencyConflict] });

        var service = CreateService();

        var result = await service.CreateLetterWithBlobAsync(LetterInput("application/pdf"));

        result.IsSuccess.Should().BeFalse();
        await _blobStorageService.Received(1).DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Scenario09b_DatabaseThrows_DeletesUploadedBlobAndReturnsError()
    {
        ConfigureValidLetterUpload();
        _repository.CreateLetterAsync(Arg.Any<CreateLetterInputDataModel>())
            .Throws(new InvalidOperationException("db down"));

        var service = CreateService();

        var result = await service.CreateLetterWithBlobAsync(LetterInput("application/pdf"));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(DocumentMessages.DocumentSaveFailed);
        await _blobStorageService.Received(1).DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Scenario13_ImprovementDatabaseUpdateFails_DeletesImprovedBlobOnly()
    {
        const long documentId = 42;
        const int reviewerId = 20;

        _userService.HasPermission(Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _repository.GetDocumentBlobContextAsync(documentId).Returns(new DocumentBlobContextDataModel
        {
            DocumentId = documentId,
            DocumentType = DocumentType.Letter,
            ChapterId = ChapterId,
            StudentId = StudentId,
            SponsorId = SponsorId,
            PlanId = PlanId,
            DigitalImprovementStatus = DigitalImprovementStatus.InProgress,
            ImprovementLockedById = reviewerId,
            Pages =
            [
                new DocumentPageBlobDataModel
                {
                    PageNumber = 1,
                    Original = new BlobPathDataModel { StoragePath = "orig.jpg", MimeType = "image/jpeg" },
                    Active = new BlobPathDataModel { StoragePath = "orig.jpg", MimeType = "image/jpeg" },
                },
            ],
        });
        _blobStorageService
            .UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => new UploadBlobResult
            {
                BlobName = callInfo.ArgAt<string>(1),
                MimeType = callInfo.ArgAt<string>(2),
                FileSizeBytes = 10,
                Sha256 = [1, 2, 3],
            });
        _repository.SubmitDigitalImprovementAsync(Arg.Any<SubmitDigitalImprovementInputDataModel>())
            .Returns([DocumentMessages.ConcurrencyConflict]);

        var service = CreateService();

        var result = await service.SubmitDigitalImprovementWithBlobAsync(new SubmitDigitalImprovementWithBlobInputModel(
            documentId, reviewerId, "Reviewer", null,
            [new UploadFileInputModel(new MemoryStream(Encoding.UTF8.GetBytes("improved")), "image/jpeg")], new byte[8]));

        result.IsSuccess.Should().BeFalse();
        await _blobStorageService.Received(1).DeleteAsync(
            Arg.Is<string>(name => name.Contains("/improved/")), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitImprovement_WithNonImageImproved_ReturnsError()
    {
        const long documentId = 42;
        const int reviewerId = 20;

        _userService.HasPermission(Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _repository.GetDocumentBlobContextAsync(documentId).Returns(new DocumentBlobContextDataModel
        {
            DocumentId = documentId,
            DocumentType = DocumentType.Letter,
            DigitalImprovementStatus = DigitalImprovementStatus.InProgress,
            ImprovementLockedById = reviewerId,
            PlanId = PlanId,
            SponsorId = SponsorId,
            Pages =
            [
                new DocumentPageBlobDataModel
                {
                    PageNumber = 1,
                    Original = new BlobPathDataModel { StoragePath = "orig.jpg", MimeType = "image/jpeg" },
                    Active = new BlobPathDataModel { StoragePath = "orig.jpg", MimeType = "image/jpeg" },
                },
            ],
        });

        var service = CreateService();

        var result = await service.SubmitDigitalImprovementWithBlobAsync(new SubmitDigitalImprovementWithBlobInputModel(
            documentId, reviewerId, "Reviewer", null,
            [new UploadFileInputModel(new MemoryStream(new byte[10]), "application/pdf")], new byte[8]));

        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().Contain(DocumentMessages.ImprovedBlobMustBeImage);
        await _blobStorageService.DidNotReceive().UploadAsync(
            Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}