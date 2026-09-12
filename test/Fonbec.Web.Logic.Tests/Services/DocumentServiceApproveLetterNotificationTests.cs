using FluentAssertions;
using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.DataModels.Documents.Input;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Models.Documents.Input;
using Fonbec.Web.Logic.Models.PlannedDeliveries;
using Fonbec.Web.Logic.Options;
using Fonbec.Web.Logic.Services;
using Mapster;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Fonbec.Web.Logic.Tests.Services;

public class DocumentServiceApproveLetterNotificationTests
{
    private const int PlanId = 10;
    private const int ChapterId = 3;
    private const long DocumentId = 55;
    private const int ReviewerId = 20;
    private static readonly DateTime PlanStartsOn = new(2026, 9, 1);

    private readonly IDocumentRepository _repository = Substitute.For<IDocumentRepository>();
    private readonly IDocumentNotificationService _notificationService = Substitute.For<IDocumentNotificationService>();
    private readonly IPlanCompletionService _planCompletionService = Substitute.For<IPlanCompletionService>();

    public DocumentServiceApproveLetterNotificationTests()
    {
        TypeAdapterConfig.GlobalSettings.Scan(typeof(DocumentService).Assembly);
    }

    [Fact]
    public async Task ApproveLetter_Emails_Managers_When_Plan_Becomes_Ready()
    {
        SetupLetter();
        _repository.ApproveLetterAsync(Arg.Any<ApproveLetterInputDataModel>())
            .Returns([]);
        _planCompletionService.GetReadinessAsync(PlanId, ChapterId, TestContext.Current.CancellationToken)
            .Returns(
                new PlanReadinessResult { PlanFound = true, PlanStartsOn = PlanStartsOn },
                new PlanReadinessResult
                {
                    PlanFound = true,
                    IsReadyToComplete = true,
                    PlanStartsOn = PlanStartsOn,
                });

        var result = await CreateService().ApproveLetterAsync(ApproveInput());

        result.IsSuccess.Should().BeTrue();
        await _notificationService.Received(1).NotifyChapterManagersPlanReadyAsync(
            ChapterId, PlanId, PlanStartsOn);
        await _planCompletionService.DidNotReceive()
            .CompletePlanAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ApproveLetter_Does_Not_Email_When_Plan_Was_Already_Ready()
    {
        SetupLetter();
        _repository.ApproveLetterAsync(Arg.Any<ApproveLetterInputDataModel>())
            .Returns([]);
        _planCompletionService.GetReadinessAsync(PlanId, ChapterId, TestContext.Current.CancellationToken)
            .Returns(new PlanReadinessResult
            {
                PlanFound = true,
                IsReadyToComplete = true,
                PlanStartsOn = PlanStartsOn,
            });

        await CreateService().ApproveLetterAsync(ApproveInput());

        await _notificationService.DidNotReceiveWithAnyArgs()
            .NotifyChapterManagersPlanReadyAsync(default, default, default);
    }

    private DocumentService CreateService() =>
        new(_repository,
            _notificationService,
            Substitute.For<IUserService>(),
            Substitute.For<IBlobStorageService>(),
            _planCompletionService,
            Microsoft.Extensions.Options.Options.Create(new BlobStorageOptions()),
            NullLogger<DocumentService>.Instance);

    private void SetupLetter() =>
        _repository.GetDocumentByIdAsync(DocumentId).Returns(new Letter
        {
            DocumentId = DocumentId,
            DocumentType = DocumentType.Letter,
            PlanId = PlanId,
            ChapterId = ChapterId,
        });

    private static ApproveLetterInputModel ApproveInput() =>
        new(
            DocumentId,
            ReviewerId,
            FonbecRole.Reviewer,
            [1, 2, 3],
            ConfirmedIsLetter: true,
            ConfirmedWrittenDate: DateTime.UtcNow.Date,
            ConfirmedAddressee: true,
            ConfirmedSignerMatchesStudent: true,
            SpellingScore: 4,
            PenmanshipScore: 4,
            ContentScore: 4,
            HasRedFlags: false,
            HasGreenFlags: false,
            IssuesNotes: null,
            Appraisal: null);
}