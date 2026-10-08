using FluentAssertions;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Options;
using Fonbec.Web.DataAccess.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Tests.Repositories;

public sealed class RecipientMessageRepositoryTests : IDisposable
{
    private const byte LetterType = (byte)DocumentType.Letter;
    private const byte ReportCardType = (byte)DocumentType.ReportCard;
    private const byte OtherType = (byte)DocumentType.Other;
    private const byte TextFileKind = (byte)FileKind.Text;
    private const byte Approved = (byte)DocumentStatus.Approved;

    private readonly SqliteConnection _connection;
    private readonly IDbContextFactory<FonbecWebDbContext> _factory;
    private readonly DocumentRepository _repository;

    private readonly Guid _sponsorToken = Guid.NewGuid();
    private readonly Guid _otherSponsorToken = Guid.NewGuid();
    private readonly Guid _companyToken = Guid.NewGuid();

    public RecipientMessageRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:;Foreign Keys=False");
        _connection.Open();
        _factory = new SqliteDbContextFactory(_connection);

        using (var db = _factory.CreateDbContext())
        {
            db.Database.EnsureCreated();
        }

        _repository = new DocumentRepository(
            _factory,
            TimeProvider.System,
            Microsoft.Extensions.Options.Options.Create(new DocumentQueueOptions()));
    }

    [Fact]
    public async Task CheckConstraint_RequiresExactlyOneRecipient()
    {
        await using var db = await _factory.CreateDbContextAsync(TestContext.Current.CancellationToken);

        db.Set<RecipientMessage>().Add(Message(sponsorId: null, companyId: null));
        var neither = () => db.SaveChangesAsync(TestContext.Current.CancellationToken);
        await neither.Should().ThrowAsync<DbUpdateException>();

        db.ChangeTracker.Clear();
        db.Set<RecipientMessage>().Add(Message(sponsorId: 1, companyId: 1));
        var both = () => db.SaveChangesAsync(TestContext.Current.CancellationToken);
        await both.Should().ThrowAsync<DbUpdateException>();

        db.ChangeTracker.Clear();
        db.Set<RecipientMessage>().Add(Message(sponsorId: 1, companyId: null, body: "persona"));
        db.Set<RecipientMessage>().Add(Message(sponsorId: null, companyId: 2, body: "empresa"));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var saved = await db.Set<RecipientMessage>().AsNoTracking().ToListAsync(TestContext.Current.CancellationToken);
        saved.Should().ContainSingle(m => m.SponsorId == 1 && m.CompanyId == null && m.Body == "persona");
        saved.Should().ContainSingle(m => m.SponsorId == null && m.CompanyId == 2 && m.Body == "empresa");
    }

    [Fact]
    public async Task Thread_IncludesOwnLettersAndMessages_AndExcludesOtherDocumentsAndRecipients()
    {
        await SeedHistoryAsync();

        var person = await _repository.GetRecipientThreadAsync(_sponsorToken, studentId: 10, isCompany: false, 0, 20);
        person.IsAuthorized.Should().BeTrue();
        person.Items.Should().HaveCount(2);
        person.Items.Select(i => i.OccurredOnUtc).Should().BeInAscendingOrder();
        person.Items[0].IsMessage.Should().BeFalse();
        person.Items[0].DocumentType.Should().Be(DocumentType.Letter);
        person.Items[1].IsMessage.Should().BeTrue();
        person.Items[1].Body.Should().Be("Del padrino");
        person.Items.Should().NotContain(i => i.DocumentType == DocumentType.ReportCard);
        person.Items.Should().NotContain(i => i.DocumentType == DocumentType.Other);
        person.Items.Should().NotContain(i => i.Body == "De la empresa");
        person.Items.Should().NotContain(i => i.Body == "De otro padrino");

        var company = await _repository.GetRecipientThreadAsync(_companyToken, studentId: 10, isCompany: true, 0, 20);
        company.IsAuthorized.Should().BeTrue();
        company.Items.Should().ContainSingle();
        company.Items[0].Body.Should().Be("De la empresa");
        company.Items.Should().NotContain(i => i.Body == "Del padrino");

        var otherSponsor = await _repository.GetRecipientThreadAsync(
            _otherSponsorToken, studentId: 10, isCompany: false, 0, 20);
        otherSponsor.Items.Should().ContainSingle();
        otherSponsor.Items[0].Body.Should().Be("De otro padrino");
    }

    [Fact]
    public async Task Thread_PagesFromTheEnd_OldestFirstWithinEachPage()
    {
        await SeedHistoryAsync();

        await using (var db = await _factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            for (var i = 0; i < 20; i++)
            {
                db.Set<RecipientMessage>().Add(Message(
                    sponsorId: 1,
                    companyId: null,
                    body: $"m{i}",
                    sentOn: new DateTime(2026, 5, 1).AddMinutes(i)));
            }

            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var newest = await _repository.GetRecipientThreadAsync(_sponsorToken, 10, false, skipFromEnd: 0, take: 20);
        newest.HasOlder.Should().BeTrue();
        newest.Items.Should().HaveCount(20);
        newest.Items.Select(i => i.OccurredOnUtc).Should().BeInAscendingOrder();
        newest.Items[0].Body.Should().Be("m0");
        newest.Items[^1].Body.Should().Be("m19");

        var older = await _repository.GetRecipientThreadAsync(_sponsorToken, 10, false, skipFromEnd: 20, take: 20);
        older.HasOlder.Should().BeFalse();
        older.Items.Should().HaveCount(2);
        older.Items[0].DocumentType.Should().Be(DocumentType.Letter);
        older.Items[1].Body.Should().Be("Del padrino");
    }

    [Fact]
    public async Task Send_UnauthorizedToken_DoesNotInsert_AndCreateLeavesSharedOnNull()
    {
        await SeedHistoryAsync();
        await using var db = await _factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var before = await db.Set<RecipientMessage>().CountAsync(TestContext.Current.CancellationToken);

        var denied = await _repository.SendRecipientMessageAsync(Guid.NewGuid(), 10, false, "No");
        denied.IsAuthorized.Should().BeFalse();
        denied.Message.Should().BeNull();

        var personOnCompanyUrl = await _repository.SendRecipientMessageAsync(_sponsorToken, 10, true, "No");
        personOnCompanyUrl.IsAuthorized.Should().BeFalse();

        var empty = await _repository.SendRecipientMessageAsync(_sponsorToken, 10, false, "   ");
        empty.IsAuthorized.Should().BeTrue();
        empty.IsValid.Should().BeFalse();

        (await db.Set<RecipientMessage>().CountAsync(TestContext.Current.CancellationToken)).Should().Be(before);

        var saved = await _repository.SendRecipientMessageAsync(_sponsorToken, 10, false, "  Hola  ");
        saved.IsAuthorized.Should().BeTrue();
        saved.IsValid.Should().BeTrue();
        saved.Message!.MessageSharedOn.Should().BeNull();
        saved.Message.Body.Should().Be("Hola");

        var row = await db.Set<RecipientMessage>().AsNoTracking()
            .SingleAsync(m => m.RecipientMessageId == saved.Message.RecipientMessageId, TestContext.Current.CancellationToken);
        row.SponsorId.Should().Be(1);
        row.CompanyId.Should().BeNull();
        row.StudentId.Should().Be(10);
        row.SharedOn.Should().BeNull();
        row.SharedById.Should().BeNull();
        row.Body.Should().Be("Hola");
    }

    public void Dispose() => _connection.Dispose();

    private async Task SeedHistoryAsync()
    {
        await using var db = await _factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var now = DateTime.UtcNow;

        db.Set<Student>().Add(new Student
        {
            Id = 10,
            FirstName = "Maria",
            LastName = "Garcia",
            ChapterId = 1,
            FacilitatorId = 1,
            CreatedById = 1,
            CreatedOnUtc = now,
            IsActive = true,
        });
        db.Set<Sponsor>().AddRange(
            new Sponsor
            {
                Id = 1,
                FirstName = "Ana",
                LastName = "Perez",
                Email = "ana@test.com",
                ChapterId = 1,
                PublicAccessToken = _sponsorToken,
                CreatedById = 1,
                CreatedOnUtc = now,
                IsActive = true,
            },
            new Sponsor
            {
                Id = 2,
                FirstName = "Luis",
                LastName = "Diaz",
                Email = "luis@test.com",
                ChapterId = 1,
                PublicAccessToken = _otherSponsorToken,
                CreatedById = 1,
                CreatedOnUtc = now,
                IsActive = true,
            });
        db.Set<Company>().Add(new Company
        {
            Id = 3,
            Name = "Acme",
            PublicAccessToken = _companyToken,
            CreatedById = 1,
            CreatedOnUtc = now,
            IsActive = true,
        });
        db.Set<Sponsorship>().AddRange(
            new Sponsorship
            {
                StudentId = 10,
                SponsorId = 1,
                StartDate = now.AddYears(-1),
                CreatedById = 1,
                CreatedOnUtc = now,
                IsActive = true,
            },
            new Sponsorship
            {
                StudentId = 10,
                SponsorId = 2,
                StartDate = now.AddYears(-1),
                CreatedById = 1,
                CreatedOnUtc = now,
                IsActive = true,
            },
            new Sponsorship
            {
                StudentId = 10,
                CompanyId = 3,
                StartDate = now.AddYears(-1),
                CreatedById = 1,
                CreatedOnUtc = now,
                IsActive = true,
            });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        await InsertDocumentAsync(db, 1, LetterType, sponsorId: 1, planId: 1, description: null, period: null);
        await InsertDocumentAsync(db, 2, ReportCardType, sponsorId: null, planId: null, description: "Boletín", period: "2026-03-01");
        await InsertDocumentAsync(db, 3, OtherType, sponsorId: null, planId: null, description: "Constancia", period: null);

        db.Set<DocumentShare>().AddRange(
            Share(documentId: 1, sponsorId: 1, companyId: null, sharedOn: new DateTime(2026, 1, 10)),
            Share(documentId: 2, sponsorId: 1, companyId: null, sharedOn: new DateTime(2026, 3, 1)),
            Share(documentId: 3, sponsorId: 1, companyId: null, sharedOn: new DateTime(2026, 4, 1)));
        db.Set<RecipientMessage>().AddRange(
            Message(sponsorId: 1, companyId: null, body: "Del padrino", sentOn: new DateTime(2026, 2, 2)),
            Message(sponsorId: 2, companyId: null, body: "De otro padrino", sentOn: new DateTime(2026, 2, 3)),
            Message(sponsorId: null, companyId: 3, body: "De la empresa", sentOn: new DateTime(2026, 2, 4)));
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static async Task InsertDocumentAsync(
        FonbecWebDbContext db,
        long documentId,
        byte documentType,
        int? sponsorId,
        int? planId,
        string? description,
        string? period)
    {
        var columns =
            "DocumentId, DocumentType, ChapterId, StudentId, FileKind, DigitalImprovementStatus, " +
            "UploadedOn, UploadedById, Status, RowVersion";
        var placeholders = "{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}";
        var args = new List<object>
        {
            documentId, documentType, 1, 10, TextFileKind, (byte)DigitalImprovementStatus.NotApplicable,
            "2026-01-01 00:00:00", 1, Approved, new byte[] { 1 },
        };
        var index = 10;

        void Add(string column, object? value)
        {
            if (value is null)
            {
                return;
            }

            columns += ", " + column;
            placeholders += ", {" + index + "}";
            args.Add(value);
            index++;
        }

        Add("SponsorId", sponsorId);
        Add("PlanId", planId);
        Add("Description", description);
        Add("Period", period);

        var sql = "INSERT INTO Documents (" + columns + ") VALUES (" + placeholders + ")";
        await db.Database.ExecuteSqlRawAsync(sql, args.ToArray());
    }

    private static DocumentShare Share(long documentId, int? sponsorId, int? companyId, DateTime sharedOn) =>
        new()
        {
            DocumentId = documentId,
            SponsorId = sponsorId,
            CompanyId = companyId,
            StudentId = 10,
            SharedOn = sharedOn,
            SharedById = 1,
        };

    private static RecipientMessage Message(
        int? sponsorId,
        int? companyId,
        string body = "hola",
        DateTime? sentOn = null) =>
        new()
        {
            StudentId = 10,
            SponsorId = sponsorId,
            CompanyId = companyId,
            Body = body,
            SentOn = sentOn ?? DateTime.UtcNow,
        };

    private sealed class SqliteDbContextFactory(SqliteConnection connection)
        : IDbContextFactory<FonbecWebDbContext>
    {
        public FonbecWebDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<FonbecWebDbContext>()
                .UseSqlite(connection)
                .Options);

        public Task<FonbecWebDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());
    }
}