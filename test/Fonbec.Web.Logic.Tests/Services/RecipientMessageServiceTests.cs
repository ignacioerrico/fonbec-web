using FluentAssertions;
using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.DataModels.Documents;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Models.Documents;
using Fonbec.Web.Logic.Options;
using Fonbec.Web.Logic.Services;
using Mapster;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Fonbec.Web.Logic.Tests.Services;

public class RecipientMessageServiceTests
{
    private readonly IDocumentRepository _repository = Substitute.For<IDocumentRepository>();
    private readonly IRecipientMessageNotificationService _notifications =
        Substitute.For<IRecipientMessageNotificationService>();
    private readonly DocumentService _service;

    private static readonly Guid Token = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    private const int StudentId = 5;

    public RecipientMessageServiceTests()
    {
        TypeAdapterConfig.GlobalSettings.Scan(typeof(DocumentService).Assembly);

        _service = new DocumentService(
            _repository,
            Substitute.For<IDocumentNotificationQueue>(),
            Substitute.For<IUserService>(),
            Substitute.For<IBlobStorageService>(),
            Substitute.For<IPlanCompletionService>(),
            Microsoft.Extensions.Options.Options.Create(new BlobStorageOptions()),
            _notifications,
            NullLogger<DocumentService>.Instance);
    }

    [Fact]
    public async Task Send_Person_Trims_KeepsLineBreaks_AndLeavesSharedOnUnset()
    {
        AuthorizePerson();
        var sentOn = new DateTime(2026, 2, 2, 21, 40, 0, DateTimeKind.Utc);
        _repository.SendRecipientMessageAsync(Token, StudentId, false, "Hola\ncómo estás")
            .Returns(SavedMessage("Hola\ncómo estás", sentOn, sharedOn: null));

        var result = await _service.SendRecipientMessageAsync(Token, StudentId, false, "  Hola\ncómo estás  ");

        result.IsAuthorized.Should().BeTrue();
        result.IsSaved.Should().BeTrue();
        result.Message!.Body.Should().Be("Hola\ncómo estás");
        result.Message.SharedOn.Should().BeNull();
        result.Message.StatusLabel.Should().Be("Pendiente");
        result.Message.Letter.Should().BeNull();
        await _repository.Received(1).SendRecipientMessageAsync(Token, StudentId, false, "Hola\ncómo estás");
        await _notifications.Received(1).NotifyFacilitatorOfRecipientMessageAsync(1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Send_Company_SavesForTheCompany()
    {
        _repository.GetRecipientHistoryAccessAsync(Token, StudentId, true)
            .Returns(new RecipientHistoryAccessDataModel { CompanyId = 9 });
        _repository.SendRecipientMessageAsync(Token, StudentId, true, "Gracias")
            .Returns(SavedMessage("Gracias", DateTime.UtcNow, sharedOn: null));

        var result = await _service.SendRecipientMessageAsync(Token, StudentId, true, "Gracias");

        result.IsSaved.Should().BeTrue();
        await _repository.Received(1).SendRecipientMessageAsync(Token, StudentId, true, "Gracias");
        await _repository.DidNotReceive().SendRecipientMessageAsync(
            Token, StudentId, false, Arg.Any<string?>());
        await _notifications.Received(1).NotifyFacilitatorOfRecipientMessageAsync(1, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   \n\t  ")]
    public async Task Send_Empty_DoesNotInsert(string? body)
    {
        AuthorizePerson();

        var result = await _service.SendRecipientMessageAsync(Token, StudentId, false, body);

        result.IsAuthorized.Should().BeTrue();
        result.IsSaved.Should().BeFalse();
        result.Message.Should().BeNull();
        await _repository.DidNotReceive().SendRecipientMessageAsync(
            Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<string?>());
        await _notifications.DidNotReceive().NotifyFacilitatorOfRecipientMessageAsync(
            Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Send_OverLimit_DoesNotInsert_AndExactLimitDoes()
    {
        AuthorizePerson();
        var exact = new string('a', MaxLength.RecipientMessage.Body);
        var over = exact + "b";
        _repository.SendRecipientMessageAsync(Token, StudentId, false, exact)
            .Returns(SavedMessage(exact, DateTime.UtcNow, sharedOn: null));

        var rejected = await _service.SendRecipientMessageAsync(Token, StudentId, false, "  " + over + "  ");
        rejected.IsSaved.Should().BeFalse();

        var saved = await _service.SendRecipientMessageAsync(Token, StudentId, false, "  " + exact + "  ");
        saved.IsSaved.Should().BeTrue();

        await _repository.DidNotReceive().SendRecipientMessageAsync(Token, StudentId, false, over);
        await _repository.Received(1).SendRecipientMessageAsync(Token, StudentId, false, exact);
        await _notifications.Received(1).NotifyFacilitatorOfRecipientMessageAsync(1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Send_WhenNotificationThrows_StillSavesAsPending()
    {
        AuthorizePerson();
        _repository.SendRecipientMessageAsync(Token, StudentId, false, "Hola")
            .Returns(SavedMessage("Hola", DateTime.UtcNow, sharedOn: null));
        _notifications
            .NotifyFacilitatorOfRecipientMessageAsync(1, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("mail down"));

        var result = await _service.SendRecipientMessageAsync(Token, StudentId, false, "Hola");

        result.IsSaved.Should().BeTrue();
        result.Message!.StatusLabel.Should().Be("Pendiente");
        result.Message.SharedOn.Should().BeNull();
    }

    [Fact]
    public async Task Send_Unauthorized_DoesNotInsert()
    {
        _repository.GetRecipientHistoryAccessAsync(Token, StudentId, false)
            .Returns((RecipientHistoryAccessDataModel?)null);

        var result = await _service.SendRecipientMessageAsync(Token, StudentId, false, "Hola");

        result.IsAuthorized.Should().BeFalse();
        result.IsSaved.Should().BeFalse();
        result.Message.Should().BeNull();
        await _repository.DidNotReceive().SendRecipientMessageAsync(
            Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<string?>());
        await _notifications.DidNotReceive().NotifyFacilitatorOfRecipientMessageAsync(
            Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetThread_IsLettersAndMessages_OldestFirst_WithoutReportCards()
    {
        var letterOn = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc);
        var messageOn = new DateTime(2026, 2, 2, 21, 40, 0, DateTimeKind.Utc);
        var deliveredOn = new DateTime(2026, 4, 3, 12, 12, 0, DateTimeKind.Utc);

        _repository.GetRecipientThreadAsync(Token, StudentId, false, 0, 20)
            .Returns(new RecipientThreadDataModel
            {
                IsAuthorized = true,
                Items =
                [
                    new RecipientThreadItemDataModel
                    {
                        IsMessage = true,
                        OccurredOnUtc = messageOn,
                        SortId = 8,
                        RecipientMessageId = 8,
                        Body = "Gracias",
                        SentOn = messageOn,
                        MessageSharedOn = deliveredOn,
                    },
                    new RecipientThreadItemDataModel
                    {
                        IsMessage = false,
                        DocumentType = DocumentType.ReportCard,
                        DocumentId = 99,
                        OccurredOnUtc = letterOn.AddDays(1),
                        SortId = 2,
                    },
                    new RecipientThreadItemDataModel
                    {
                        IsMessage = false,
                        DocumentType = DocumentType.Letter,
                        DocumentId = 4,
                        FileKind = FileKind.Text,
                        OccurredOnUtc = letterOn,
                        SortId = 1,
                        PlanStartsOn = new DateTime(2026, 1, 1),
                        TextContent = "Carta",
                    },
                ],
            });

        var thread = await _service.GetRecipientThreadAsync(Token, StudentId, false, 0, 20);

        thread.IsAuthorized.Should().BeTrue();
        thread.Items.Should().HaveCount(2);
        thread.Items.Select(i => i.OccurredOnUtc).Should().BeInAscendingOrder();
        thread.Items[0].IsMessage.Should().BeFalse();
        var letter = thread.Items[0].Letter;
        letter.Should().NotBeNull();
        letter!.DocumentType.Should().Be(DocumentType.Letter);
        letter.Title.Should().StartWith("Carta de ");
        thread.Items[1].IsMessage.Should().BeTrue();
        thread.Items[1].Body.Should().Be("Gracias");
        thread.Items[1].StatusLabel.Should().Be($"Entregado {deliveredOn.ToLocalTime().ToLocalizedDateTime()}");
        thread.Items.Should().NotContain(i => i.Letter != null && i.Letter.DocumentType == DocumentType.ReportCard);
    }

    [Fact]
    public async Task GetThread_Unauthorized_ReturnsNoItems()
    {
        _repository.GetRecipientThreadAsync(Token, StudentId, true, 0, 20)
            .Returns(new RecipientThreadDataModel { IsAuthorized = false, Items = [new RecipientThreadItemDataModel()] });

        var thread = await _service.GetRecipientThreadAsync(Token, StudentId, true, 0, 20);

        thread.IsAuthorized.Should().BeFalse();
        thread.Items.Should().BeEmpty();
    }

    [Fact]
    public void StatusLabel_Pending_HasNoDeliveredTime_AndDelivered_HidesWhoMarkedIt()
    {
        var pending = new RecipientThreadItemViewModel { IsMessage = true };
        pending.StatusLabel.Should().Be("Pendiente");
        pending.StatusLabel.Should().NotContain("2026");

        var sharedOn = new DateTime(2026, 4, 3, 12, 12, 0, DateTimeKind.Utc);
        var delivered = new RecipientThreadItemViewModel { IsMessage = true, SharedOn = sharedOn };
        delivered.StatusLabel.Should().Be($"Entregado {sharedOn.ToLocalTime().ToLocalizedDateTime()}");

        var undone = new RecipientThreadItemViewModel { IsMessage = true, SharedOn = null };
        undone.StatusLabel.Should().Be("Pendiente");
    }

    private void AuthorizePerson() =>
        _repository.GetRecipientHistoryAccessAsync(Token, StudentId, false)
            .Returns(new RecipientHistoryAccessDataModel { SponsorId = 7 });

    private static SendRecipientMessageDataModel SavedMessage(string body, DateTime sentOn, DateTime? sharedOn) =>
        new()
        {
            IsAuthorized = true,
            IsValid = true,
            Message = new RecipientThreadItemDataModel
            {
                IsMessage = true,
                OccurredOnUtc = sentOn,
                SortId = 1,
                RecipientMessageId = 1,
                Body = body,
                SentOn = sentOn,
                MessageSharedOn = sharedOn,
            },
        };
}