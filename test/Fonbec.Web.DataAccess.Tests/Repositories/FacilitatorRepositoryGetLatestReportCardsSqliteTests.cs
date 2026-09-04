using FluentAssertions;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Fonbec.Web.DataAccess.Tests.Repositories;

// Exercises GetLatestReportCardsAsync against a real relational provider (SQLite) so the
// top-N-per-student query is verified to translate to SQL, rather than being evaluated in
// memory as it is under the EF Core InMemory provider.
//
// Report cards are seeded with raw SQL because the store-generated RowVersion (a SQL Server
// rowversion) is excluded from EF inserts and has no SQLite default; foreign keys are disabled
// so parent rows (chapter/student/user) are not required. The goal is validating query
// translation, not referential integrity.
public sealed class FacilitatorRepositoryGetLatestReportCardsSqliteTests : IDisposable
{
    private const byte ReportCardDiscriminator = (byte)DocumentType.ReportCard;
    private const byte TextFileKind = (byte)FileKind.Text;
    private const byte NotApplicableImprovement = (byte)DigitalImprovementStatus.NotApplicable;

    private readonly SqliteConnection _connection;
    private readonly IDbContextFactory<FonbecWebDbContext> _factory;

    public FacilitatorRepositoryGetLatestReportCardsSqliteTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:;Foreign Keys=False");
        _connection.Open();

        _factory = new SqliteDbContextFactory(_connection);

        using var db = _factory.CreateDbContext();
        db.Database.EnsureCreated();
    }

    [Fact]
    public async Task GetLatestReportCardsAsync_Returns_Latest_Three_Per_Student_Ordered_Newest_First()
    {
        await using (var db = await _factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            await SeedReportCardAsync(db, documentId: 1, studentId: 10, year: 2026, month: 3);
            await SeedReportCardAsync(db, documentId: 2, studentId: 10, year: 2026, month: 4);
            await SeedReportCardAsync(db, documentId: 3, studentId: 10, year: 2026, month: 5);
            await SeedReportCardAsync(db, documentId: 4, studentId: 10, year: 2026, month: 6);
            await SeedReportCardAsync(db, documentId: 5, studentId: 11, year: 2026, month: 4);
            await SeedReportCardAsync(db, documentId: 6, studentId: 11, year: 2026, month: 5);
            await SeedReportCardAsync(db, documentId: 7, studentId: 11, year: 2026, month: 6);
        }

        var repository = new FacilitatorRepository(_factory, TimeProvider.System);

        var result = await repository.GetLatestReportCardsAsync([10, 11], 3);

        result.Should().HaveCount(6);

        result.Where(r => r.StudentId == 10)
            .Select(r => r.Period)
            .Should()
            .Equal(
                new DateOnly(2026, 6, 1),
                new DateOnly(2026, 5, 1),
                new DateOnly(2026, 4, 1));

        result.Count(r => r.StudentId == 11).Should().Be(3);
    }

    [Fact]
    public async Task GetLatestReportCardsAsync_Maps_RejectionReason_From_RejectedReason_Description()
    {
        await using (var db = await _factory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            // Seeded RejectedReason Id 7 = "No es boletín o libreta" (AppliesToDocumentType = ReportCard).
            await SeedReportCardAsync(db, documentId: 1, studentId: 10, year: 2026, month: 6,
                status: DocumentStatus.Rejected, rejectedReasonId: 7);
        }

        var repository = new FacilitatorRepository(_factory, TimeProvider.System);

        var result = await repository.GetLatestReportCardsAsync([10], 3);

        result.Single().RejectionReason.Should().Be("No es boletín o libreta");
    }

    private static async Task SeedReportCardAsync(
        FonbecWebDbContext db,
        long documentId,
        int studentId,
        int year,
        int month,
        DocumentStatus status = DocumentStatus.Approved,
        int? rejectedReasonId = null)
    {
        var period = new DateOnly(year, month, 1).ToString("yyyy-MM-dd");
        var uploadedOn = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc).ToString("yyyy-MM-dd HH:mm:ss");

        var columns =
            "DocumentId, DocumentType, ChapterId, StudentId, FileKind, DigitalImprovementStatus, " +
            "UploadedOn, UploadedById, Status, RowVersion, Period, Description";
        var placeholders = "{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}";
        var args = new List<object>
        {
            documentId, ReportCardDiscriminator, 1, studentId, TextFileKind, NotApplicableImprovement,
            uploadedOn, 1, (byte)status, new byte[] { 1 }, period, $"Report {month}/{year}",
        };

        // A nullable foreign key is omitted rather than passed as DBNull, which the raw-SQL API
        // cannot map to a store type.
        if (rejectedReasonId is { } reasonId)
        {
            columns += ", RejectedReasonId";
            placeholders += ", {12}";
            args.Add(reasonId);
        }

        // Built by concatenation (not an interpolated string) so EF1002 does not flag this call;
        // all runtime values are still passed as positional parameters below.
        var sql = "INSERT INTO Documents (" + columns + ") VALUES (" + placeholders + ")";

        await db.Database.ExecuteSqlRawAsync(sql, args.ToArray(), TestContext.Current.CancellationToken);
    }

    public void Dispose() => _connection.Dispose();

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