using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Authorization;
using Fonbec.Web.Logic.Constants;
using Fonbec.Web.Logic.Models.Documents.Input;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Logic.Util;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace Fonbec.Web.DataAccess.Tests.Integration.Documents;

internal sealed class DocumentTestFixture
{
    private readonly string _databaseName = Guid.NewGuid().ToString();

    public int ChapterId { get; private set; }
    public int UploaderId { get; private set; }
    public int OtherUploaderId { get; private set; }
    public int ReviewerId { get; private set; }
    public int ManagerId { get; private set; }
    public int StudentId { get; private set; }
    public int SponsorAId { get; private set; }
    public int SponsorBId { get; private set; }
    public Guid SponsorAToken { get; private set; }
    public int PlanId { get; private set; }

    public IDbContextFactory<FonbecWebDbContext> Factory { get; private set; } = null!;
    public IDocumentRepository DocumentRepository { get; private set; } = null!;
    public IDocumentService DocumentService { get; private set; } = null!;
    public IEmailMessageSender EmailSender { get; private set; } = null!;

    public async Task InitializeAsync(bool includeActivePlan = true)
    {
        Factory = CreateDbContextFactory();
        await SeedAsync(includeActivePlan);

        DocumentRepository = new DocumentRepository(Factory);
        EmailSender = Substitute.For<IEmailMessageSender>();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["App:BaseUrl"] = "https://fonbec.test",
                ["Email:From"] = "noreply@fonbec.test",
            })
            .Build();

        var notificationService = new DocumentNotificationService(DocumentRepository, EmailSender, configuration);
        var pages = new List<PageAccessInfo>
        {
            new(DocumentPermission.DigitalImprovement, "Digital improvement", ["Reviewer", "Manager"]),
        };
        var userService = new UserService(
            Substitute.For<IUserRepository>(),
            Substitute.For<IPasswordGeneratorWrapper>(),
            EmailSender,
            pages);

        TypeAdapterConfig.GlobalSettings.Scan(typeof(DocumentService).Assembly);
        DocumentService = new DocumentService(DocumentRepository, notificationService, userService);
    }

    public CreateDocumentUserContext UploaderContext => new(UploaderId, "Uploader", ChapterId, null);

    public CreateDocumentUserContext OtherUploaderUserContext =>
        new(OtherUploaderId, "Uploader", ChapterId, null);

    public async Task<Document> GetDocumentAsync(long documentId)
    {
        await using var db = await Factory.CreateDbContextAsync();
        return (await db.Set<Document>().FindAsync(documentId))!;
    }

    private async Task SeedAsync(bool includeActivePlan)
    {
        await using var db = await Factory.CreateDbContextAsync();

        ChapterId = 1;
        UploaderId = 1;
        OtherUploaderId = 4;
        ReviewerId = 2;
        ManagerId = 3;
        StudentId = 1;
        SponsorAId = 1;
        SponsorBId = 2;
        SponsorAToken = Guid.NewGuid();

        var chapter = new Chapter
        {
            Id = ChapterId,
            Name = "Test Chapter",
            CreatedById = UploaderId,
            CreatedOnUtc = DateTime.UtcNow,
            IsActive = true,
        };

        var users = new[]
        {
            CreateUser(UploaderId, "uploader"),
            CreateUser(ReviewerId, "reviewer"),
            CreateUser(ManagerId, "manager"),
            CreateUser(OtherUploaderId, "otheruploader"),
        };

        var student = new Student
        {
            Id = StudentId,
            FirstName = "Maria",
            LastName = "Garcia",
            ChapterId = ChapterId,
            FacilitatorId = UploaderId,
            CreatedById = UploaderId,
            CreatedOnUtc = DateTime.UtcNow,
            IsActive = true,
        };

        var sponsorA = new Sponsor
        {
            Id = SponsorAId,
            FirstName = "John",
            LastName = "Smith",
            Email = "sponsor.a@test.com",
            ChapterId = ChapterId,
            PublicAccessToken = SponsorAToken,
            CreatedById = UploaderId,
            CreatedOnUtc = DateTime.UtcNow,
            IsActive = true,
        };

        var sponsorB = new Sponsor
        {
            Id = SponsorBId,
            FirstName = "Jane",
            LastName = "Doe",
            Email = "sponsor.b@test.com",
            ChapterId = ChapterId,
            PublicAccessToken = Guid.NewGuid(),
            CreatedById = UploaderId,
            CreatedOnUtc = DateTime.UtcNow,
            IsActive = true,
        };

        var utcNow = DateTime.UtcNow;
        var sponsorshipA = new Sponsorship
        {
            Id = 1,
            StudentId = StudentId,
            SponsorId = SponsorAId,
            StartDate = utcNow.AddYears(-1),
            CreatedById = UploaderId,
            CreatedOnUtc = utcNow,
            IsActive = true,
        };

        var sponsorshipB = new Sponsorship
        {
            Id = 2,
            StudentId = StudentId,
            SponsorId = SponsorBId,
            StartDate = utcNow.AddYears(-1),
            CreatedById = UploaderId,
            CreatedOnUtc = utcNow,
            IsActive = true,
        };

        db.Set<Chapter>().Add(chapter);
        db.Users.AddRange(users);
        db.Set<Student>().Add(student);
        db.Set<Sponsor>().AddRange(sponsorA, sponsorB);
        db.Set<Sponsorship>().AddRange(sponsorshipA, sponsorshipB);

        if (includeActivePlan)
        {
            var plan = new PlannedDelivery
            {
                Id = 1,
                ChapterId = ChapterId,
                StartsOn = utcNow.AddDays(-7),
                Completed = false,
                CreatedById = UploaderId,
                CreatedOnUtc = utcNow,
                IsActive = true,
            };
            db.Set<PlannedDelivery>().Add(plan);
            PlanId = plan.Id;
        }

        await db.SaveChangesAsync();
    }

    private static FonbecWebUser CreateUser(int id, string name) => new()
    {
        Id = id,
        UserName = name,
        NormalizedUserName = name.ToUpperInvariant(),
        Email = $"{name}@fonbec.test",
        NormalizedEmail = $"{name}@fonbec.test".ToUpperInvariant(),
        FirstName = name,
        LastName = "User",
        SecurityStamp = Guid.NewGuid().ToString(),
        ChapterId = id is 1 or 3 or 4 ? 1 : null,
    };

    private IDbContextFactory<FonbecWebDbContext> CreateDbContextFactory() =>
        new TestDbContextFactory(_databaseName);

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