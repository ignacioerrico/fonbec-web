using FluentAssertions;
using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.DataModels.RecipientMessages;
using Fonbec.Web.DataAccess.DataModels.Users.Output;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.ExtensionMethods;
using Fonbec.Web.Logic.Services;
using NSubstitute;

namespace Fonbec.Web.Logic.Tests.Services;

public class RecipientMessageQueueTests
{
    private const int UploaderId = 11;
    private const int ManagerId = 22;
    private static readonly DateTimeOffset UtcNow = new(2026, 4, 2, 14, 5, 0, TimeSpan.Zero);

    private readonly IRecipientMessageRepository _repository = Substitute.For<IRecipientMessageRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly RecipientMessageService _service;

    public RecipientMessageQueueTests()
    {
        _service = new RecipientMessageService(_repository, _users, new FixedTimeProvider(UtcNow));
    }

    [Fact]
    public async Task GetForActor_Uploader_LoadsOnlyThatFacilitatorsStudents()
    {
        Actor(UploaderId, FonbecRole.Uploader, chapterId: 9);
        var older = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);
        var newer = new DateTime(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc);
        _repository.GetForFacilitatorAsync(UploaderId).Returns(
        [
            Item(2, newer, student: "María López", sender: "Ana Pérez"),
            Item(1, older, student: "Juan García", sender: "Acme SA", company: true, body: "Hola\ncómo estás"),
        ]);

        var result = await _service.GetForActorAsync(UploaderId);

        await _repository.Received(1).GetForFacilitatorAsync(UploaderId);
        await _repository.DidNotReceive().GetForChapterAsync(Arg.Any<int>());
        result.Pending.Select(m => m.RecipientMessageId).Should().Equal(1, 2);
        result.Delivered.Should().BeEmpty();
        result.Pending[0].StudentFullName.Should().Be("Juan García");
        result.Pending[0].Body.Should().Be("Hola\ncómo estás");
    }

    [Fact]
    public async Task GetForActor_Manager_UsesTheChapterStoredForThatUser()
    {
        Actor(ManagerId, FonbecRole.Manager, chapterId: 4);
        _repository.GetForChapterAsync(4).Returns([Item(8, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc))]);

        var result = await _service.GetForActorAsync(ManagerId);

        result.Pending.Should().ContainSingle(m => m.RecipientMessageId == 8);
        await _repository.Received(1).GetForChapterAsync(4);
        await _repository.DidNotReceive().GetForFacilitatorAsync(Arg.Any<int>());
        await _repository.DidNotReceive().GetForChapterAsync(Arg.Is<int>(id => id != 4));
    }

    [Theory]
    [InlineData(FonbecRole.Admin)]
    [InlineData(FonbecRole.Reviewer)]
    public async Task GetForActor_OtherRoles_SeeNothing(string role)
    {
        Actor(5, role, chapterId: 1);

        var result = await _service.GetForActorAsync(5);

        result.Pending.Should().BeEmpty();
        result.Delivered.Should().BeEmpty();
        await _repository.DidNotReceive().GetForFacilitatorAsync(Arg.Any<int>());
        await _repository.DidNotReceive().GetForChapterAsync(Arg.Any<int>());
    }

    [Fact]
    public async Task GetForActor_ManagerWithoutChapter_SeesNothing()
    {
        Actor(ManagerId, FonbecRole.Manager, chapterId: null);

        var result = await _service.GetForActorAsync(ManagerId);

        result.Pending.Should().BeEmpty();
        await _repository.DidNotReceive().GetForChapterAsync(Arg.Any<int>());
    }

    [Fact]
    public async Task GetForActor_LabelsPersonAndCompany_AndOrdersBothSections()
    {
        Actor(UploaderId, FonbecRole.Uploader, chapterId: 1);
        var firstSent = new DateTime(2026, 1, 2, 15, 0, 0, DateTimeKind.Utc);
        var secondSent = new DateTime(2026, 1, 2, 15, 0, 0, DateTimeKind.Utc);
        var olderShared = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc);
        var newerShared = new DateTime(2026, 3, 2, 10, 0, 0, DateTimeKind.Utc);
        _repository.GetForFacilitatorAsync(UploaderId).Returns(
        [
            Item(20, secondSent, sender: "Ana Pérez"),
            Item(10, firstSent, sender: "Acme SA", company: true),
            Item(40, firstSent, sharedOn: olderShared, sharedBy: "Luis Gómez"),
            Item(30, secondSent, sharedOn: newerShared, sharedBy: "Ana Mediadora"),
            Item(50, secondSent, sharedOn: newerShared, sharedBy: "Ana Mediadora"),
        ]);

        var result = await _service.GetForActorAsync(UploaderId);

        result.Pending.Select(m => m.RecipientMessageId).Should().Equal(10, 20);
        result.Pending[0].SenderKindLabel.Should().Be("Empresa");
        result.Pending[0].SenderName.Should().Be("Acme SA");
        result.Pending[0].SentOnLabel.Should().Be(firstSent.ToLocalTime().ToLocalizedDateTime());
        result.Pending[1].SenderKindLabel.Should().Be("Padrino");
        result.Pending[1].SenderName.Should().Be("Ana Pérez");

        result.Delivered.Select(m => m.RecipientMessageId).Should().Equal(50, 30, 40);
        result.Delivered[0].SharedOnLabel.Should().Be(newerShared.ToLocalTime().ToLocalizedDateTime());
        result.Delivered[0].SharedByFullName.Should().Be("Ana Mediadora");
    }

    [Fact]
    public async Task MarkShared_SetsWhoAndWhen()
    {
        Actor(UploaderId, FonbecRole.Uploader, chapterId: 1);
        _repository.GetInScopeAsync(8, Arg.Is<RecipientMessageScope.Facilitator>(s => s.UserId == UploaderId))
            .Returns(new RecipientMessageShareStateDataModel());
        _repository.SetSharedAsync(
                8,
                Arg.Is<RecipientMessageScope.Facilitator>(s => s.UserId == UploaderId),
                UploaderId,
                UtcNow.UtcDateTime)
            .Returns(true);

        var saved = await _service.MarkSharedAsync(8, UploaderId);

        saved.Should().BeTrue();
        await _repository.Received(1).SetSharedAsync(
            8,
            Arg.Is<RecipientMessageScope.Facilitator>(s => s.UserId == UploaderId),
            UploaderId,
            UtcNow.UtcDateTime);
    }

    [Fact]
    public async Task MarkShared_AlreadyShared_DoesNotChangeWhoOrWhen()
    {
        Actor(ManagerId, FonbecRole.Manager, chapterId: 4);
        _repository.GetInScopeAsync(8, Arg.Is<RecipientMessageScope.Chapter>(s => s.ChapterId == 4))
            .Returns(new RecipientMessageShareStateDataModel
            {
                SharedOn = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                SharedById = 99,
            });

        var saved = await _service.MarkSharedAsync(8, ManagerId);

        saved.Should().BeTrue();
        await _repository.DidNotReceive().SetSharedAsync(
            Arg.Any<long>(), Arg.Any<RecipientMessageScope>(), Arg.Any<int>(), Arg.Any<DateTime>());
    }

    [Fact]
    public async Task UndoShared_ClearsTheMark_ForEitherRole()
    {
        Actor(ManagerId, FonbecRole.Manager, chapterId: 4);
        _repository.GetInScopeAsync(8, Arg.Is<RecipientMessageScope.Chapter>(s => s.ChapterId == 4))
            .Returns(new RecipientMessageShareStateDataModel
            {
                SharedOn = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                SharedById = UploaderId,
            });
        _repository.ClearSharedAsync(8, Arg.Is<RecipientMessageScope.Chapter>(s => s.ChapterId == 4))
            .Returns(true);

        var cleared = await _service.UndoSharedAsync(8, ManagerId);

        cleared.Should().BeTrue();
        await _repository.Received(1).ClearSharedAsync(
            8, Arg.Is<RecipientMessageScope.Chapter>(s => s.ChapterId == 4));
    }

    [Fact]
    public async Task UndoShared_AlreadyPending_DoesNotWrite()
    {
        Actor(UploaderId, FonbecRole.Uploader, chapterId: 1);
        _repository.GetInScopeAsync(8, Arg.Any<RecipientMessageScope>())
            .Returns(new RecipientMessageShareStateDataModel());

        var cleared = await _service.UndoSharedAsync(8, UploaderId);

        cleared.Should().BeTrue();
        await _repository.DidNotReceive().ClearSharedAsync(Arg.Any<long>(), Arg.Any<RecipientMessageScope>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MarkAndUndo_OutsideScope_AreDenied(bool mark)
    {
        Actor(UploaderId, FonbecRole.Uploader, chapterId: 1);
        _repository.GetInScopeAsync(99, Arg.Any<RecipientMessageScope>())
            .Returns((RecipientMessageShareStateDataModel?)null);

        var saved = mark
            ? await _service.MarkSharedAsync(99, UploaderId)
            : await _service.UndoSharedAsync(99, UploaderId);

        saved.Should().BeFalse();
        await _repository.DidNotReceive().SetSharedAsync(
            Arg.Any<long>(), Arg.Any<RecipientMessageScope>(), Arg.Any<int>(), Arg.Any<DateTime>());
        await _repository.DidNotReceive().ClearSharedAsync(Arg.Any<long>(), Arg.Any<RecipientMessageScope>());
    }

    [Fact]
    public async Task MarkShared_UnknownUser_IsDenied()
    {
        _users.GetUserAsync(7).Returns((GetUserOutputDataModel?)null);

        var saved = await _service.MarkSharedAsync(1, 7);

        saved.Should().BeFalse();
        await _repository.DidNotReceive().GetInScopeAsync(Arg.Any<long>(), Arg.Any<RecipientMessageScope>());
    }

    private void Actor(int userId, string role, int? chapterId)
    {
        _users.GetUserAsync(userId).Returns(new GetUserOutputDataModel
        {
            ChapterId = chapterId,
            UserFullName = "Actor",
            UserRole = role,
        });
    }

    private static StudentMessageQueueItemDataModel Item(
        long id,
        DateTime sentOn,
        string student = "Juan García",
        string sender = "Ana Pérez",
        bool company = false,
        string body = "Gracias",
        DateTime? sharedOn = null,
        string? sharedBy = null) =>
        new()
        {
            RecipientMessageId = id,
            StudentFullName = student,
            IsCompany = company,
            SenderName = sender,
            Body = body,
            SentOn = sentOn,
            SharedOn = sharedOn,
            SharedByFullName = sharedBy,
        };

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}