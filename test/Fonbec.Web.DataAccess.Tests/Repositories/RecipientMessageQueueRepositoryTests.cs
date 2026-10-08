using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.RecipientMessages;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Tests.Repositories;

public sealed class RecipientMessageQueueRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly IDbContextFactory<FonbecWebDbContext> _factory;
    private readonly RecipientMessageRepository _repository;

    public RecipientMessageQueueRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:;Foreign Keys=False");
        _connection.Open();
        _factory = new SqliteDbContextFactory(_connection);

        using (var db = _factory.CreateDbContext())
        {
            db.Database.EnsureCreated();
        }

        _repository = new RecipientMessageRepository(_factory);
    }

    [Fact]
    public async Task Queue_UsesCurrentFacilitator_AndChapter()
    {
        var mine = await AddMessageAsync(
            studentId: 10, facilitatorId: 1, chapterId: 1, body: "mío");
        await AddMessageAsync(
            studentId: 11, facilitatorId: 2, chapterId: 1, body: "otro mediador");
        await AddMessageAsync(
            studentId: 12, facilitatorId: 3, chapterId: 2, body: "otra filial");

        var facilitator = await _repository.GetForFacilitatorAsync(1);
        facilitator.Should().ContainSingle(m => m.RecipientMessageId == mine && m.Body == "mío");

        var chapter = await _repository.GetForChapterAsync(1);
        chapter.Select(m => m.Body).Should().BeEquivalentTo("mío", "otro mediador");

        var otherChapter = await _repository.GetForChapterAsync(2);
        otherChapter.Should().ContainSingle(m => m.Body == "otra filial");
    }

    [Fact]
    public async Task Queue_FollowsFacilitatorChange()
    {
        var messageId = await AddMessageAsync(
            studentId: 10, facilitatorId: 1, chapterId: 1, body: "para el becario");

        (await _repository.GetForFacilitatorAsync(1)).Should().ContainSingle(m => m.RecipientMessageId == messageId);
        (await _repository.GetForFacilitatorAsync(2)).Should().BeEmpty();

        await using (var db = await _factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            var student = await db.Set<Student>().SingleAsync(s => s.Id == 10, TestContext.Current.CancellationToken);
            student.FacilitatorId = 2;
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        (await _repository.GetForFacilitatorAsync(2)).Should().ContainSingle(m => m.RecipientMessageId == messageId);
        (await _repository.GetForFacilitatorAsync(1)).Should().BeEmpty();
        (await _repository.GetForChapterAsync(1)).Should().ContainSingle(m => m.RecipientMessageId == messageId);
    }

    [Fact]
    public async Task Queue_ProjectsPersonAndCompanySenders_AndWhoDelivered()
    {
        await AddMessageAsync(
            studentId: 10,
            facilitatorId: 1,
            chapterId: 1,
            studentFirst: "Juan",
            studentLast: "García",
            body: "del padrino",
            sponsorId: 5,
            sponsorFirst: "Ana",
            sponsorLast: "Pérez");
        var deliveredOn = new DateTime(2026, 3, 2, 19, 20, 0, DateTimeKind.Utc);
        await AddMessageAsync(
            studentId: 11,
            facilitatorId: 1,
            chapterId: 1,
            studentFirst: "María",
            studentLast: "López",
            body: "de la empresa",
            companyId: 8,
            companyName: "Acme SA",
            sharedOn: deliveredOn,
            sharedById: 7,
            sharedByFirst: "Luis",
            sharedByLast: "Gómez");

        var rows = await _repository.GetForFacilitatorAsync(1);

        var person = rows.Should().ContainSingle(m => m.Body == "del padrino").Subject;
        person.IsCompany.Should().BeFalse();
        person.StudentFullName.Should().Be("Juan García");
        person.SenderName.Should().Be("Ana Pérez");
        person.SharedOn.Should().BeNull();
        person.SharedByFullName.Should().BeNull();

        var company = rows.Should().ContainSingle(m => m.Body == "de la empresa").Subject;
        company.IsCompany.Should().BeTrue();
        company.StudentFullName.Should().Be("María López");
        company.SenderName.Should().Be("Acme SA");
        company.SharedOn.Should().Be(deliveredOn);
        company.SharedByFullName.Should().Be("Luis Gómez");
    }

    [Fact]
    public async Task SetShared_PersistsWhoAndWhen_AndDoesNotOverwrite()
    {
        var messageId = await AddMessageAsync(studentId: 10, facilitatorId: 1, chapterId: 1, body: "hola");
        var when = new DateTime(2026, 4, 2, 19, 20, 0, DateTimeKind.Utc);
        var scope = new RecipientMessageScope.Facilitator(1);

        var saved = await _repository.SetSharedAsync(messageId, scope, actorUserId: 7, when);
        var again = await _repository.SetSharedAsync(
            messageId,
            scope,
            actorUserId: 8,
            new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc));

        saved.Should().BeTrue();
        again.Should().BeTrue();
        var row = await ReadAsync(messageId);
        row.SharedOn.Should().Be(when);
        row.SharedById.Should().Be(7);
    }

    [Fact]
    public async Task ClearShared_ClearsBoth_AndIsIdempotent()
    {
        var when = new DateTime(2026, 4, 2, 19, 20, 0, DateTimeKind.Utc);
        var messageId = await AddMessageAsync(
            studentId: 10, facilitatorId: 1, chapterId: 1, body: "hola", sharedOn: when, sharedById: 7);
        var scope = new RecipientMessageScope.Chapter(1);

        var cleared = await _repository.ClearSharedAsync(messageId, scope);
        var again = await _repository.ClearSharedAsync(messageId, scope);

        cleared.Should().BeTrue();
        again.Should().BeTrue();
        var row = await ReadAsync(messageId);
        row.SharedOn.Should().BeNull();
        row.SharedById.Should().BeNull();
    }

    [Fact]
    public async Task SetSharedAndClearShared_DenyOutOfScope()
    {
        var when = new DateTime(2026, 4, 2, 19, 20, 0, DateTimeKind.Utc);
        var pendingId = await AddMessageAsync(studentId: 10, facilitatorId: 1, chapterId: 1, body: "pendiente");
        var deliveredId = await AddMessageAsync(
            studentId: 11,
            facilitatorId: 1,
            chapterId: 1,
            body: "entregado",
            sharedOn: when,
            sharedById: 7);

        var mark = await _repository.SetSharedAsync(
            pendingId, new RecipientMessageScope.Facilitator(2), actorUserId: 2, when);
        var undo = await _repository.ClearSharedAsync(deliveredId, new RecipientMessageScope.Chapter(2));
        var missing = await _repository.SetSharedAsync(
            999, new RecipientMessageScope.Facilitator(1), actorUserId: 1, when);

        mark.Should().BeFalse();
        undo.Should().BeFalse();
        missing.Should().BeFalse();
        (await _repository.GetInScopeAsync(pendingId, new RecipientMessageScope.Facilitator(2))).Should().BeNull();
        (await _repository.GetInScopeAsync(pendingId, new RecipientMessageScope.Facilitator(1)))!
            .SharedOn.Should().BeNull();

        var pending = await ReadAsync(pendingId);
        pending.SharedOn.Should().BeNull();
        pending.SharedById.Should().BeNull();

        var delivered = await ReadAsync(deliveredId);
        delivered.SharedOn.Should().Be(when);
        delivered.SharedById.Should().Be(7);
    }

    public void Dispose() => _connection.Dispose();

    private async Task<long> AddMessageAsync(
        int studentId,
        int facilitatorId,
        int chapterId,
        string body,
        string studentFirst = "Juan",
        string studentLast = "García",
        int? sponsorId = null,
        string sponsorFirst = "Ana",
        string sponsorLast = "Pérez",
        int? companyId = null,
        string? companyName = null,
        DateTime? sharedOn = null,
        int? sharedById = null,
        string sharedByFirst = "Luis",
        string sharedByLast = "Gómez")
    {
        await using var db = await _factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        if (!await db.Set<Student>().AnyAsync(s => s.Id == studentId, TestContext.Current.CancellationToken))
        {
            db.Set<Student>().Add(new Student
            {
                Id = studentId,
                FirstName = studentFirst,
                LastName = studentLast,
                ChapterId = chapterId,
                FacilitatorId = facilitatorId,
                CreatedById = 1,
                CreatedOnUtc = now,
                IsActive = true,
            });
        }

        if (companyId is int company && !await db.Set<Company>().AnyAsync(c => c.Id == company, TestContext.Current.CancellationToken))
        {
            db.Set<Company>().Add(new Company
            {
                Id = company,
                Name = companyName ?? "Empresa",
                PublicAccessToken = Guid.NewGuid(),
                CreatedById = 1,
                CreatedOnUtc = now,
                IsActive = true,
            });
        }

        int? resolvedSponsorId = companyId is null ? sponsorId ?? studentId : null;
        if (resolvedSponsorId is int sponsor && !await db.Set<Sponsor>().AnyAsync(s => s.Id == sponsor, TestContext.Current.CancellationToken))
        {
            db.Set<Sponsor>().Add(new Sponsor
            {
                Id = sponsor,
                FirstName = sponsorFirst,
                LastName = sponsorLast,
                ChapterId = chapterId,
                PublicAccessToken = Guid.NewGuid(),
                CreatedById = 1,
                CreatedOnUtc = now,
                IsActive = true,
            });
        }

        if (sharedById is int userId && !await db.Users.AnyAsync(u => u.Id == userId, TestContext.Current.CancellationToken))
        {
            db.Users.Add(new FonbecWebUser
            {
                Id = userId,
                FirstName = sharedByFirst,
                LastName = sharedByLast,
                Email = $"user{userId}@example.org",
                UserName = $"user{userId}@example.org",
                CreatedOnUtc = now,
            });
        }

        var message = new RecipientMessage
        {
            StudentId = studentId,
            SponsorId = resolvedSponsorId,
            CompanyId = companyId,
            Body = body,
            SentOn = now,
            SharedOn = sharedOn,
            SharedById = sharedById,
        };
        db.Set<RecipientMessage>().Add(message);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        return message.RecipientMessageId;
    }

    private async Task<RecipientMessage> ReadAsync(long messageId)
    {
        await using var db = await _factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        return await db.Set<RecipientMessage>().AsNoTracking()
            .SingleAsync(m => m.RecipientMessageId == messageId, TestContext.Current.CancellationToken);
    }

    private sealed class SqliteDbContextFactory(SqliteConnection connection) : IDbContextFactory<FonbecWebDbContext>
    {
        public FonbecWebDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<FonbecWebDbContext>()
                .UseSqlite(connection)
                .Options);

        public Task<FonbecWebDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());
    }
}