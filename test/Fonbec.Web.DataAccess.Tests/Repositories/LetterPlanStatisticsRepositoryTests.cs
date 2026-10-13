using FluentAssertions;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Fonbec.Web.DataAccess.Tests.Repositories;

public class LetterPlanStatisticsRepositoryTests
{
    private const int ChapterId = 1;
    private const int OtherChapterId = 2;
    private const int PlanId = 100;
    private const int UserId = 2;
    private const int SponsorId = 20;
    private const int LinkedSponsorId = 21;
    private const int CompanyId = 40;
    private const int StudentId = 10;
    private static readonly DateTime UtcNow = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime PlanStartsOn = new(2026, 3, 1);

    [Fact]
    public async Task GetStatisticsAsync_Returns_Null_When_Plan_Belongs_To_Other_Chapter()
    {
        var factory = CreateFactory();
        await SeedPlanAsync(factory, chapterId: OtherChapterId, completed: true);

        var result = await CreateRepository(factory).GetStatisticsAsync(PlanId, ChapterId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetStatisticsAsync_Returns_Null_When_Plan_Is_Still_Open()
    {
        var factory = CreateFactory();
        await SeedPlanAsync(factory, ChapterId, completed: false);

        var result = await CreateRepository(factory).GetStatisticsAsync(PlanId, ChapterId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetStatisticsAsync_Uses_Addressee_Share_Instead_Of_Fan_Out()
    {
        var factory = CreateFactory();
        await SeedPlanAsync(factory, ChapterId, completed: true);
        await SeedCompanyLetterAsync(factory);

        var result = await CreateRepository(factory).GetStatisticsAsync(PlanId, ChapterId);

        result.Should().NotBeNull();
        result!.Letters.Should().ContainSingle();
        result.Letters[0].AddresseeNotificationSentOn.Should().Be(UtcNow.AddDays(3));
        result.Letters[0].ReviewerName.Should().Be("Ana Pérez");
        result.ExemptionCount.Should().Be(1);
    }

    private static LetterPlanStatisticsRepository CreateRepository(TestDbContextFactory factory) =>
        new(factory);

    private static TestDbContextFactory CreateFactory() => new(Guid.NewGuid().ToString());

    private static async Task SeedPlanAsync(TestDbContextFactory factory, int chapterId, bool completed)
    {
        await using var db = await factory.CreateDbContextAsync();

        db.Set<Chapter>().Add(new Chapter
        {
            Id = chapterId,
            Name = "Córdoba",
            CreatedById = 1,
            CreatedOnUtc = UtcNow,
            IsActive = true,
        });

        db.Users.Add(new FonbecWebUser
        {
            Id = UserId,
            UserName = "reviewer",
            NormalizedUserName = "REVIEWER",
            FirstName = "Ana",
            LastName = "Pérez",
            SecurityStamp = Guid.NewGuid().ToString(),
        });

        db.Set<Company>().Add(new Company
        {
            Id = CompanyId,
            Name = "Acme",
            CreatedById = 1,
            CreatedOnUtc = UtcNow,
            IsActive = true,
        });

        db.Set<Sponsor>().AddRange(
            new Sponsor
            {
                Id = SponsorId,
                FirstName = "María",
                LastName = "López",
                Gender = Gender.Unknown,
                ChapterId = chapterId,
                CreatedById = 1,
                CreatedOnUtc = UtcNow,
                IsActive = true,
            },
            new Sponsor
            {
                Id = LinkedSponsorId,
                FirstName = "Juan",
                LastName = "Ruiz",
                Gender = Gender.Unknown,
                CompanyId = CompanyId,
                ChapterId = chapterId,
                CreatedById = 1,
                CreatedOnUtc = UtcNow,
                IsActive = true,
            });

        db.Set<PlannedDelivery>().Add(new PlannedDelivery
        {
            Id = PlanId,
            ChapterId = chapterId,
            StartsOn = PlanStartsOn,
            Completed = completed,
            CreatedById = 1,
            CreatedOnUtc = UtcNow,
        });

        db.Set<Student>().Add(new Student
        {
            Id = StudentId,
            FirstName = "Juan",
            LastName = "García",
            Gender = Gender.Unknown,
            ChapterId = chapterId,
            FacilitatorId = UserId,
            CreatedById = 1,
            CreatedOnUtc = UtcNow,
            IsActive = true,
        });

        if (completed)
        {
            db.Set<LetterExemption>().Add(new LetterExemption
            {
                StudentId = StudentId,
                PlannedDeliveryId = PlanId,
                ChapterId = chapterId,
                Reason = "Viaje",
                CreatedByFonbecUserId = UserId,
                CreatedOnUtc = UtcNow,
            });
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedCompanyLetterAsync(TestDbContextFactory factory)
    {
        await using var db = await factory.CreateDbContextAsync();

        var assessment = new Assessment
        {
            AssessmentId = 1,
            SpellingScore = 4,
            PenmanshipScore = 4,
            ContentScore = 4,
        };
        db.Set<Assessment>().Add(assessment);

        db.Set<Letter>().Add(new Letter
        {
            DocumentId = 1,
            DocumentType = DocumentType.Letter,
            ChapterId = ChapterId,
            StudentId = StudentId,
            CompanyId = CompanyId,
            PlanId = PlanId,
            FileKind = FileKind.Text,
            TextContent = "Carta",
            UploadedOn = UtcNow,
            UploadedById = UserId,
            Status = DocumentStatus.Approved,
            ApprovedOn = UtcNow.AddDays(1),
            RowVersion = [1, 0, 0, 0, 0, 0, 0, 0],
            Review = new LetterReview
            {
                LetterReviewId = 1,
                AssessmentId = assessment.AssessmentId,
                ConfirmedWrittenDate = PlanStartsOn,
                ReviewedById = UserId,
                ReviewedOn = UtcNow.AddDays(1),
            },
            Shares =
            [
                new DocumentShare
                {
                    CompanyId = CompanyId,
                    StudentId = StudentId,
                    SharedOn = UtcNow.AddDays(1),
                    SharedById = UserId,
                    NotificationSentOn = UtcNow.AddDays(3),
                },
                new DocumentShare
                {
                    SponsorId = LinkedSponsorId,
                    StudentId = StudentId,
                    SharedOn = UtcNow.AddDays(1),
                    SharedById = UserId,
                    NotificationSentOn = UtcNow.AddDays(9),
                },
            ],
        });

        await db.SaveChangesAsync();
    }

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