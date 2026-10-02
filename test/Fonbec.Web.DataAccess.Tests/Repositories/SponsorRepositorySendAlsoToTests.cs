using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Sponsors.Input;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Fonbec.Web.DataAccess.Tests.Repositories;

public class SponsorRepositorySendAlsoToTests
{
    [Fact]
    public async Task CreateSponsorAsync_Persists_Recipients_With_The_Sponsor()
    {
        var factory = CreateDbContextFactory();
        await SeedAsync(factory);
        var repository = new SponsorRepository(factory);

        var affectedRows = await repository.CreateSponsorAsync(new CreateSponsorInputDataModel
        {
            ChapterId = 1,
            SponsorFirstName = "Ana",
            SponsorLastName = "Perez",
            SponsorEmail = "ana@ejemplo.com",
            CreatedById = 1,
            SendAlsoTos =
            [
                new CreateSendAlsoToInputDataModel
                {
                    RecipientName = "Luis Perez",
                    RecipientEmail = "luis@ejemplo.com",
                    SendAsBcc = false,
                },
                new CreateSendAlsoToInputDataModel
                {
                    RecipientName = "Maria Perez",
                    RecipientEmail = "maria@ejemplo.com",
                    SendAsBcc = true,
                },
            ],
        });

        affectedRows.Should().BeGreaterThan(1);
        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var sponsor = await db.Set<Sponsor>()
            .Include(s => s.SendAlsoTos)
            .SingleAsync(TestContext.Current.CancellationToken);
        sponsor.SendAlsoTos.Should().HaveCount(2);
        sponsor.SendAlsoTos.Should().Contain(r => r.RecipientEmail == "luis@ejemplo.com" && !r.SendAsBcc);
        sponsor.SendAlsoTos.Should().Contain(r => r.RecipientEmail == "maria@ejemplo.com" && r.SendAsBcc);
        sponsor.SendAlsoTos.Should().OnlyContain(r => r.SponsorId == sponsor.Id && r.CreatedById == 1);
    }

    [Fact]
    public async Task CreateSponsorAsync_Persists_No_Recipients_When_List_Is_Empty()
    {
        var factory = CreateDbContextFactory();
        await SeedAsync(factory);
        var repository = new SponsorRepository(factory);

        await repository.CreateSponsorAsync(new CreateSponsorInputDataModel
        {
            ChapterId = 1,
            SponsorFirstName = "Ana",
            SponsorLastName = "Perez",
            SponsorEmail = "ana@ejemplo.com",
            CreatedById = 1,
        });

        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        db.Set<SendAlsoTo>().Should().BeEmpty();
        db.Set<Sponsor>().Should().ContainSingle();
    }

    [Fact]
    public async Task GetAllSponsorsAsync_Lists_Cc_Recipients_Before_Bcc_Each_Alphabetically()
    {
        var factory = CreateDbContextFactory();
        await SeedAsync(factory);
        var repository = new SponsorRepository(factory);

        await repository.CreateSponsorAsync(new CreateSponsorInputDataModel
        {
            ChapterId = 1,
            SponsorFirstName = "Ana",
            SponsorLastName = "Perez",
            SponsorEmail = "ana@ejemplo.com",
            CreatedById = 1,
            SendAlsoTos =
            [
                new CreateSendAlsoToInputDataModel
                {
                    RecipientName = "Zoe Diaz",
                    RecipientEmail = "zoe@ejemplo.com",
                    SendAsBcc = true,
                },
                new CreateSendAlsoToInputDataModel
                {
                    RecipientName = "Carlos Ruiz",
                    RecipientEmail = "carlos@ejemplo.com",
                    SendAsBcc = false,
                },
                new CreateSendAlsoToInputDataModel
                {
                    RecipientName = "Bruno Lopez",
                    RecipientEmail = "bruno@ejemplo.com",
                    SendAsBcc = true,
                },
                new CreateSendAlsoToInputDataModel
                {
                    RecipientName = "Ana Gomez",
                    RecipientEmail = "ana.gomez@ejemplo.com",
                    SendAsBcc = false,
                },
            ],
        });

        var sponsors = await repository.GetAllSponsorsAsync(chapterId: null);

        sponsors.Should().ContainSingle();
        sponsors[0].SendAlsoTos.Select(r => (r.Name, r.SendAsBcc)).Should().Equal(
            ("Ana Gomez", false),
            ("Carlos Ruiz", false),
            ("Bruno Lopez", true),
            ("Zoe Diaz", true));
    }

    [Fact]
    public async Task UpdateSendAlsoTosAsync_Adds_Edits_And_Removes()
    {
        var factory = CreateDbContextFactory();
        var seeded = await SeedSponsorWithRecipientsAsync(factory);
        var repository = new SponsorRepository(factory);

        var result = await repository.UpdateSendAlsoTosAsync(new UpdateSponsorSendAlsoTosInputDataModel
        {
            SponsorId = seeded.SponsorId,
            ChapterId = 1,
            UpdatedById = 1,
            Recipients =
            [
                new UpdateSendAlsoToInputDataModel
                {
                    Id = seeded.LuisId,
                    RecipientName = "Luis Gomez",
                    RecipientEmail = "luis.gomez@ejemplo.com",
                    SendAsBcc = false,
                },
                new UpdateSendAlsoToInputDataModel
                {
                    Id = 0,
                    RecipientName = "Carlos Diaz",
                    RecipientEmail = "carlos@ejemplo.com",
                    SendAsBcc = true,
                },
            ],
        });

        result.SponsorFound.Should().BeTrue();
        result.Rejected.Should().BeFalse();
        result.AffectedRows.Should().BeGreaterThan(0);

        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var recipients = await db.Set<SendAlsoTo>()
            .Where(r => r.SponsorId == seeded.SponsorId)
            .ToListAsync(TestContext.Current.CancellationToken);
        recipients.Should().HaveCount(2);
        recipients.Should().NotContain(r => r.Id == seeded.MariaId);
        var luis = recipients.Should().ContainSingle(r => r.Id == seeded.LuisId).Subject;
        luis.RecipientName.Should().Be("Luis Gomez");
        luis.RecipientEmail.Should().Be("luis.gomez@ejemplo.com");
        luis.LastUpdatedById.Should().Be(1);
        recipients.Should().Contain(r => r.RecipientEmail == "carlos@ejemplo.com" && r.SendAsBcc && r.CreatedById == 1);
    }

    [Fact]
    public async Task GetSendAlsoTosBySponsorIdAsync_Is_Limited_To_The_Caller_Chapter()
    {
        var factory = CreateDbContextFactory();
        var seeded = await SeedSponsorWithRecipientsAsync(factory, chapterId: 2);
        var repository = new SponsorRepository(factory);

        var otherChapter = await repository.GetSendAlsoTosBySponsorIdAsync(seeded.SponsorId, chapterId: 1);
        var sameChapter = await repository.GetSendAlsoTosBySponsorIdAsync(seeded.SponsorId, chapterId: 2);
        var anyChapter = await repository.GetSendAlsoTosBySponsorIdAsync(seeded.SponsorId, chapterId: null);

        otherChapter.Should().BeNull();
        sameChapter.Should().NotBeNull();
        sameChapter!.SponsorEmail.Should().Be("ana@ejemplo.com");
        sameChapter.Recipients.Should().HaveCount(2);
        anyChapter.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateSendAlsoTosAsync_Does_Not_Change_Another_Chapter_Or_Inactive_Sponsor()
    {
        var factory = CreateDbContextFactory();
        var otherChapter = await SeedSponsorWithRecipientsAsync(factory, chapterId: 2);
        var inactive = await SeedSponsorWithRecipientsAsync(factory, chapterId: 1, isActive: false, email: "inactivo@ejemplo.com");
        var repository = new SponsorRepository(factory);

        var wrongChapter = await repository.UpdateSendAlsoTosAsync(new UpdateSponsorSendAlsoTosInputDataModel
        {
            SponsorId = otherChapter.SponsorId,
            ChapterId = 1,
            UpdatedById = 1,
            Recipients = [],
        });
        var inactiveResult = await repository.UpdateSendAlsoTosAsync(new UpdateSponsorSendAlsoTosInputDataModel
        {
            SponsorId = inactive.SponsorId,
            ChapterId = 1,
            UpdatedById = 1,
            Recipients = [],
        });

        wrongChapter.SponsorFound.Should().BeFalse();
        inactiveResult.SponsorFound.Should().BeFalse();

        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var otherChapterCount = await db.Set<SendAlsoTo>()
            .CountAsync(r => r.SponsorId == otherChapter.SponsorId, TestContext.Current.CancellationToken);
        var inactiveCount = await db.Set<SendAlsoTo>()
            .CountAsync(r => r.SponsorId == inactive.SponsorId, TestContext.Current.CancellationToken);
        otherChapterCount.Should().Be(2);
        inactiveCount.Should().Be(2);
    }

    private static TestDbContextFactory CreateDbContextFactory() =>
        new(Guid.NewGuid().ToString());

    private static async Task SeedAsync(TestDbContextFactory factory)
    {
        await using var db = await factory.CreateDbContextAsync();
        db.Users.Add(new FonbecWebUser
        {
            Id = 1,
            UserName = "admin",
            NormalizedUserName = "ADMIN",
            Email = "admin@test.com",
            NormalizedEmail = "ADMIN@TEST.COM",
            FirstName = "Admin",
            LastName = "User",
            SecurityStamp = Guid.NewGuid().ToString(),
        });
        db.Set<Chapter>().Add(new Chapter
        {
            Id = 1,
            Name = "Norte",
            CreatedById = 1,
            CreatedOnUtc = DateTime.UtcNow,
            IsActive = true,
        });
        db.Set<Chapter>().Add(new Chapter
        {
            Id = 2,
            Name = "Sur",
            CreatedById = 1,
            CreatedOnUtc = DateTime.UtcNow,
            IsActive = true,
        });
        await db.SaveChangesAsync();
    }

    private static async Task<SeededSponsor> SeedSponsorWithRecipientsAsync(
        TestDbContextFactory factory,
        int chapterId = 1,
        bool isActive = true,
        string email = "ana@ejemplo.com")
    {
        await using var db = await factory.CreateDbContextAsync();
        if (!await db.Users.AnyAsync())
        {
            await SeedAsync(factory);
        }

        var sponsor = new Sponsor
        {
            FirstName = "Ana",
            LastName = "Perez",
            Email = email,
            ChapterId = chapterId,
            CreatedById = 1,
            PublicAccessToken = Guid.NewGuid(),
            SendAlsoTos =
            [
                new SendAlsoTo
                {
                    RecipientName = "Luis Perez",
                    RecipientEmail = "luis@ejemplo.com",
                    SendAsBcc = false,
                    CreatedById = 1,
                },
                new SendAlsoTo
                {
                    RecipientName = "Maria Perez",
                    RecipientEmail = "maria@ejemplo.com",
                    SendAsBcc = true,
                    CreatedById = 1,
                },
            ],
        };

        db.Set<Sponsor>().Add(sponsor);
        await db.SaveChangesAsync();

        if (!isActive)
        {
            sponsor.DisabledById = 1;
            await db.SaveChangesAsync();
        }

        var luis = sponsor.SendAlsoTos.Single(r => r.RecipientEmail == "luis@ejemplo.com");
        var maria = sponsor.SendAlsoTos.Single(r => r.RecipientEmail == "maria@ejemplo.com");
        return new SeededSponsor(sponsor.Id, luis.Id, maria.Id);
    }

    private sealed record SeededSponsor(int SponsorId, int LuisId, int MariaId);

    private sealed class TestDbContextFactory(string databaseName) : IDbContextFactory<FonbecWebDbContext>
    {
        public FonbecWebDbContext CreateDbContext() => new(CreateOptions());

        public Task<FonbecWebDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());

        private DbContextOptions<FonbecWebDbContext> CreateOptions() =>
            new DbContextOptionsBuilder<FonbecWebDbContext>()
                .UseInMemoryDatabase(databaseName)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
    }
}