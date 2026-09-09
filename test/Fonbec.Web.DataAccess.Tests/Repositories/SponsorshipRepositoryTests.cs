using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Sponsorships.Input;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Fonbec.Web.DataAccess.Tests.Repositories;

public class SponsorshipRepositoryTests
{
    private const int StudentId = 10;
    private const int SponsorId = 20;
    private const int OtherSponsorId = 21;
    private const int CompanyId = 30;
    private const int UserId = 40;

    [Fact]
    public async Task CreateSponsorshipAsync_Rejects_Overlapping_Period()
    {
        var factory = CreateDbContextFactory();
        await SeedSponsorshipAsync(factory, January(2026), EndOfMonth(2026, 6));
        var repository = new SponsorshipRepository(factory);
        var input = SponsorInput(new DateTime(2026, 5, 15), endDate: null);

        var preview = await repository.GetSponsorshipPeriodMatchAsync(input);
        var result = await repository.CreateSponsorshipAsync(input);

        preview.Should().Be(SponsorshipPeriodMatch.Overlap);
        result.PeriodMatch.Should().Be(SponsorshipPeriodMatch.Overlap);
        result.AffectedRows.Should().Be(0);
        (await GetActiveSponsorshipsAsync(factory)).Should().ContainSingle();
    }

    [Fact]
    public async Task CreateSponsorshipAsync_Rejects_Period_Overlapping_OpenEnded_Sponsorship()
    {
        var factory = CreateDbContextFactory();
        await SeedSponsorshipAsync(factory, January(2026), endDate: null);
        var repository = new SponsorshipRepository(factory);

        var result = await repository.CreateSponsorshipAsync(
            SponsorInput(new DateTime(2027, 1, 1), EndOfMonth(2027, 3)));

        result.PeriodMatch.Should().Be(SponsorshipPeriodMatch.Overlap);
        result.AffectedRows.Should().Be(0);
    }

    [Fact]
    public async Task CreateSponsorshipAsync_Extends_Adjacent_Period_Forward()
    {
        var factory = CreateDbContextFactory();
        await SeedSponsorshipAsync(factory, January(2026), EndOfMonth(2026, 6));
        var repository = new SponsorshipRepository(factory);
        var input = SponsorInput(new DateTime(2026, 7, 12), new DateTime(2026, 9, 3));

        var preview = await repository.GetSponsorshipPeriodMatchAsync(input);
        var result = await repository.CreateSponsorshipAsync(input);

        preview.Should().Be(SponsorshipPeriodMatch.Adjacent);
        result.PeriodMatch.Should().Be(SponsorshipPeriodMatch.Adjacent);
        result.AffectedRows.Should().BeGreaterThan(0);
        var sponsorship = (await GetActiveSponsorshipsAsync(factory)).Should().ContainSingle().Which;
        sponsorship.StartDate.Should().Be(January(2026));
        sponsorship.EndDate.Should().Be(EndOfMonth(2026, 9));
    }

    [Fact]
    public async Task CreateSponsorshipAsync_Extends_Adjacent_Period_Backward()
    {
        var factory = CreateDbContextFactory();
        await SeedSponsorshipAsync(factory, new DateTime(2026, 7, 1), EndOfMonth(2026, 9));
        var repository = new SponsorshipRepository(factory);

        var result = await repository.CreateSponsorshipAsync(
            SponsorInput(January(2026), EndOfMonth(2026, 6)));

        result.PeriodMatch.Should().Be(SponsorshipPeriodMatch.Adjacent);
        var sponsorship = (await GetActiveSponsorshipsAsync(factory)).Should().ContainSingle().Which;
        sponsorship.StartDate.Should().Be(January(2026));
        sponsorship.EndDate.Should().Be(EndOfMonth(2026, 9));
    }

    [Fact]
    public async Task CreateSponsorshipAsync_Merges_Two_Periods_When_New_Period_Bridges_Them()
    {
        var factory = CreateDbContextFactory();
        await SeedSponsorshipAsync(factory, January(2026), EndOfMonth(2026, 3));
        await SeedSponsorshipAsync(factory, new DateTime(2026, 7, 1), EndOfMonth(2026, 9));
        var repository = new SponsorshipRepository(factory);

        var result = await repository.CreateSponsorshipAsync(
            SponsorInput(new DateTime(2026, 4, 1), EndOfMonth(2026, 6)));

        result.PeriodMatch.Should().Be(SponsorshipPeriodMatch.Adjacent);
        var sponsorship = (await GetActiveSponsorshipsAsync(factory)).Should().ContainSingle().Which;
        sponsorship.StartDate.Should().Be(January(2026));
        sponsorship.EndDate.Should().Be(EndOfMonth(2026, 9));
    }

    [Fact]
    public async Task CreateSponsorshipAsync_Creates_Separate_NonContiguous_Period()
    {
        var factory = CreateDbContextFactory();
        await SeedSponsorshipAsync(factory, January(2026), EndOfMonth(2026, 6));
        var repository = new SponsorshipRepository(factory);

        var result = await repository.CreateSponsorshipAsync(
            SponsorInput(new DateTime(2026, 8, 1), EndOfMonth(2026, 9)));

        result.PeriodMatch.Should().Be(SponsorshipPeriodMatch.None);
        result.AffectedRows.Should().BeGreaterThan(0);
        (await GetActiveSponsorshipsAsync(factory)).Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateSponsorshipAsync_Allows_Same_Period_For_Different_Sponsor()
    {
        var factory = CreateDbContextFactory();
        await SeedSponsorshipAsync(factory, January(2026), EndOfMonth(2026, 6));
        var repository = new SponsorshipRepository(factory);

        var result = await repository.CreateSponsorshipAsync(
            SponsorInput(January(2026), EndOfMonth(2026, 6), OtherSponsorId));

        result.PeriodMatch.Should().Be(SponsorshipPeriodMatch.None);
        (await GetActiveSponsorshipsAsync(factory)).Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateSponsorshipAsync_Applies_Extension_Rule_To_Company()
    {
        var factory = CreateDbContextFactory();
        await SeedSponsorshipAsync(
            factory,
            January(2026),
            EndOfMonth(2026, 6),
            sponsorId: null,
            companyId: CompanyId);
        var repository = new SponsorshipRepository(factory);

        var result = await repository.CreateSponsorshipAsync(
            CompanyInput(new DateTime(2026, 7, 1), EndOfMonth(2026, 9)));

        result.PeriodMatch.Should().Be(SponsorshipPeriodMatch.Adjacent);
        var sponsorship = (await GetActiveSponsorshipsAsync(factory)).Should().ContainSingle().Which;
        sponsorship.EndDate.Should().Be(EndOfMonth(2026, 9));
    }

    [Fact]
    public async Task CreateSponsorshipAsync_Preserves_Existing_Notes_When_Extension_Notes_Are_Blank()
    {
        var factory = CreateDbContextFactory();
        await SeedSponsorshipAsync(
            factory,
            January(2026),
            EndOfMonth(2026, 6),
            notes: "Nota original");
        var repository = new SponsorshipRepository(factory);
        var input = SponsorInput(new DateTime(2026, 7, 1), EndOfMonth(2026, 9));
        input.SponsorshipNotes = " ";

        await repository.CreateSponsorshipAsync(input);

        var sponsorship = (await GetActiveSponsorshipsAsync(factory)).Should().ContainSingle().Which;
        sponsorship.Notes.Should().Be("Nota original");
    }

    [Fact]
    public async Task CreateSponsorshipAsync_Replaces_Existing_Notes_When_Extension_Notes_Are_Provided()
    {
        var factory = CreateDbContextFactory();
        await SeedSponsorshipAsync(
            factory,
            January(2026),
            EndOfMonth(2026, 6),
            notes: "Nota original");
        var repository = new SponsorshipRepository(factory);
        var input = SponsorInput(new DateTime(2026, 7, 1), EndOfMonth(2026, 9));
        input.SponsorshipNotes = "Nota nueva";

        await repository.CreateSponsorshipAsync(input);

        var sponsorship = (await GetActiveSponsorshipsAsync(factory)).Should().ContainSingle().Which;
        sponsorship.Notes.Should().Be("Nota nueva");
    }

    private static TestDbContextFactory CreateDbContextFactory() =>
        new(Guid.NewGuid().ToString());

    private static CreateSponsorshipInputDataModel SponsorInput(
        DateTime startDate,
        DateTime? endDate,
        int sponsorId = SponsorId) =>
        new()
        {
            StudentId = StudentId,
            SponsorId = sponsorId,
            SponsorshipStartDate = startDate,
            SponsorshipEndDate = endDate,
            CreatedById = UserId,
        };

    private static CreateSponsorshipInputDataModel CompanyInput(
        DateTime startDate,
        DateTime? endDate) =>
        new()
        {
            StudentId = StudentId,
            CompanyId = CompanyId,
            SponsorshipStartDate = startDate,
            SponsorshipEndDate = endDate,
            CreatedById = UserId,
        };

    private static async Task SeedSponsorshipAsync(
        IDbContextFactory<FonbecWebDbContext> factory,
        DateTime startDate,
        DateTime? endDate,
        int? sponsorId = SponsorId,
        int? companyId = null,
        string? notes = null)
    {
        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        db.Set<Sponsorship>().Add(new Sponsorship
        {
            StudentId = StudentId,
            SponsorId = sponsorId,
            CompanyId = companyId,
            StartDate = startDate,
            EndDate = endDate,
            Notes = notes,
            CreatedById = UserId,
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<List<Sponsorship>> GetActiveSponsorshipsAsync(
        IDbContextFactory<FonbecWebDbContext> factory)
    {
        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        return await db.Set<Sponsorship>()
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.StartDate)
            .ToListAsync(TestContext.Current.CancellationToken);
    }

    private static DateTime January(int year) => new(year, 1, 1);

    private static DateTime EndOfMonth(int year, int month) =>
        new(year, month, DateTime.DaysInMonth(year, month));

    private sealed class TestDbContextFactory(string databaseName)
        : IDbContextFactory<FonbecWebDbContext>
    {
        public FonbecWebDbContext CreateDbContext() => new(CreateOptions());

        public Task<FonbecWebDbContext> CreateDbContextAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());

        private DbContextOptions<FonbecWebDbContext> CreateOptions() =>
            new DbContextOptionsBuilder<FonbecWebDbContext>()
                .UseInMemoryDatabase(databaseName)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
    }
}