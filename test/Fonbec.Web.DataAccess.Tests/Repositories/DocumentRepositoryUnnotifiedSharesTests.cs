using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Documents;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Options;
using Fonbec.Web.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Fonbec.Web.DataAccess.Tests.Repositories;

public class DocumentRepositoryUnnotifiedSharesTests
{
    [Fact]
    public async Task GetUnnotifiedSharesAsync_Includes_SendAlsoTo_For_Person_Shares()
    {
        var factory = CreateDbContextFactory();
        var seeded = await SeedAsync(factory);
        var repository = CreateRepository(factory);

        var shares = await repository.GetUnnotifiedSharesAsync(seeded.DocumentId);

        var person = shares.Should().ContainSingle(s => s.DocumentShareId == seeded.PersonShareId).Subject;
        person.IsCompany.Should().BeFalse();
        person.RecipientEmail.Should().Be("ana@ejemplo.com");
        person.AdditionalRecipients.Should().BeEquivalentTo(
        [
            new SendAlsoToNotificationDataModel
            {
                RecipientName = "Luis Perez",
                RecipientEmail = "luis@ejemplo.com",
                SendAsBcc = false,
            },
            new SendAlsoToNotificationDataModel
            {
                RecipientName = "Marta Gomez",
                RecipientEmail = "marta@ejemplo.com",
                SendAsBcc = true,
            },
        ]);

        shares.Should().NotContain(s => s.DocumentShareId == seeded.NotifiedShareId);
        shares.Should().NotContain(s => s.DocumentShareId == seeded.OtherDocumentShareId);
    }

    [Fact]
    public async Task GetUnnotifiedSharesAsync_Leaves_Company_Additional_Recipients_Empty()
    {
        var factory = CreateDbContextFactory();
        var seeded = await SeedAsync(factory);
        var repository = CreateRepository(factory);

        var shares = await repository.GetUnnotifiedSharesAsync(seeded.DocumentId);

        var company = shares.Should().ContainSingle(s => s.DocumentShareId == seeded.CompanyShareId).Subject;
        company.IsCompany.Should().BeTrue();
        company.RecipientEmail.Should().Be("empresa@ejemplo.com");
        company.AdditionalRecipients.Should().BeEmpty();

        var sponsorWithoutCopies = shares.Should().ContainSingle(s => s.DocumentShareId == seeded.PersonWithoutCopiesShareId).Subject;
        sponsorWithoutCopies.AdditionalRecipients.Should().BeEmpty();
    }

    private static DocumentRepository CreateRepository(IDbContextFactory<FonbecWebDbContext> factory) =>
        new(factory, TimeProvider.System, Microsoft.Extensions.Options.Options.Create(new DocumentQueueOptions()));

    private static async Task<SeededShares> SeedAsync(TestDbContextFactory factory)
    {
        await using var db = await factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        db.Users.Add(new FonbecWebUser
        {
            Id = 1,
            UserName = "manager",
            NormalizedUserName = "MANAGER",
            Email = "manager@fonbec.test",
            NormalizedEmail = "MANAGER@FONBEC.TEST",
            FirstName = "Manager",
            LastName = "Test",
            SecurityStamp = Guid.NewGuid().ToString(),
        });
        db.Set<Chapter>().Add(new Chapter
        {
            Id = 1,
            Name = "Norte",
            CreatedById = 1,
        });
        db.Set<Student>().Add(new Student
        {
            Id = 7,
            FirstName = "María",
            LastName = "García",
            NickName = "Mari",
            Gender = Gender.Female,
            ChapterId = 1,
            FacilitatorId = 1,
            CreatedById = 1,
        });

        var sponsor = new Sponsor
        {
            FirstName = "Ana",
            LastName = "Perez",
            Email = "ana@ejemplo.com",
            ChapterId = 1,
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
                    RecipientName = "Marta Gomez",
                    RecipientEmail = "marta@ejemplo.com",
                    SendAsBcc = true,
                    CreatedById = 1,
                },
            ],
        };
        var sponsorWithoutCopies = new Sponsor
        {
            FirstName = "Nora",
            LastName = "Diaz",
            Email = "nora@ejemplo.com",
            ChapterId = 1,
            CreatedById = 1,
            PublicAccessToken = Guid.NewGuid(),
        };
        var notifiedSponsor = new Sponsor
        {
            FirstName = "Elena",
            LastName = "Ruiz",
            Email = "elena@ejemplo.com",
            ChapterId = 1,
            CreatedById = 1,
            PublicAccessToken = Guid.NewGuid(),
        };
        db.Set<Sponsor>().AddRange(sponsor, sponsorWithoutCopies, notifiedSponsor);
        db.Set<Company>().Add(new Company
        {
            Id = 3,
            Name = "Acme SA",
            Email = "empresa@ejemplo.com",
            PublicAccessToken = Guid.NewGuid(),
            CreatedById = 1,
        });

        var document = new OtherDocument
        {
            ChapterId = 1,
            StudentId = 7,
            FileKind = FileKind.Text,
            TextContent = "hola",
            Description = "Constancia",
            UploadedOn = DateTime.UtcNow,
            UploadedById = 1,
            Status = DocumentStatus.Approved,
            RowVersion = [1, 0, 0, 0, 0, 0, 0, 0],
        };
        var otherDocument = new OtherDocument
        {
            ChapterId = 1,
            StudentId = 7,
            FileKind = FileKind.Text,
            TextContent = "otro",
            Description = "Otro",
            UploadedOn = DateTime.UtcNow,
            UploadedById = 1,
            Status = DocumentStatus.Approved,
            RowVersion = [1, 0, 0, 0, 0, 0, 0, 0],
        };
        db.Set<OtherDocument>().AddRange(document, otherDocument);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var personShare = new DocumentShare
        {
            DocumentId = document.DocumentId,
            SponsorId = sponsor.Id,
            StudentId = 7,
            SharedOn = DateTime.UtcNow,
            SharedById = 1,
        };
        var personWithoutCopies = new DocumentShare
        {
            DocumentId = document.DocumentId,
            SponsorId = sponsorWithoutCopies.Id,
            StudentId = 7,
            SharedOn = DateTime.UtcNow,
            SharedById = 1,
        };
        var companyShare = new DocumentShare
        {
            DocumentId = document.DocumentId,
            CompanyId = 3,
            StudentId = 7,
            SharedOn = DateTime.UtcNow,
            SharedById = 1,
        };
        var notifiedShare = new DocumentShare
        {
            DocumentId = document.DocumentId,
            SponsorId = notifiedSponsor.Id,
            StudentId = 7,
            SharedOn = DateTime.UtcNow,
            SharedById = 1,
            NotificationSentOn = DateTime.UtcNow,
        };
        var otherDocumentShare = new DocumentShare
        {
            DocumentId = otherDocument.DocumentId,
            SponsorId = sponsor.Id,
            StudentId = 7,
            SharedOn = DateTime.UtcNow,
            SharedById = 1,
        };
        db.Set<DocumentShare>().AddRange(
            personShare, personWithoutCopies, companyShare, notifiedShare, otherDocumentShare);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        return new SeededShares(
            document.DocumentId,
            personShare.DocumentShareId,
            personWithoutCopies.DocumentShareId,
            companyShare.DocumentShareId,
            notifiedShare.DocumentShareId,
            otherDocumentShare.DocumentShareId);
    }

    private static TestDbContextFactory CreateDbContextFactory() => new(Guid.NewGuid().ToString());

    private sealed record SeededShares(
        long DocumentId,
        long PersonShareId,
        long PersonWithoutCopiesShareId,
        long CompanyShareId,
        long NotifiedShareId,
        long OtherDocumentShareId);

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