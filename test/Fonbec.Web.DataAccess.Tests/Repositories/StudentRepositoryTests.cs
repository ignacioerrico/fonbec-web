using FluentAssertions;
using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.DataModels.Students.Input;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Fonbec.Web.DataAccess.Tests.Repositories;

public class StudentRepositoryTests
{
    [Fact]
    public async Task CreateStudentAsync_WithValidFacilitator_CreatesStudent()
    {
        await using var fixture = await CreateFixtureAsync();

        var input = new CreateStudentInputDataModel
        {
            ChapterId = 1,
            FacilitatorId = fixture.FacilitatorId,
            StudentFirstName = "Test",
            StudentLastName = "Student",
            CreatedById = fixture.ManagerId,
        };

        var result = await fixture.Repository.CreateStudentAsync(input);

        result.Should().Be(1);

        await using var verifyDb = await fixture.DbContextFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var student = await verifyDb.Set<Student>().FirstOrDefaultAsync(TestContext.Current.CancellationToken);
        student.Should().NotBeNull();
        student.ChapterId.Should().Be(1);
        student.FacilitatorId.Should().Be(fixture.FacilitatorId);
    }

    [Fact]
    public async Task CreateStudentAsync_WithFacilitatorFromDifferentChapter_ThrowsInvalidOperationException()
    {
        await using var fixture = await CreateFixtureAsync();

        var input = new CreateStudentInputDataModel
        {
            ChapterId = 99, // Different from facilitator's chapter (1)
            FacilitatorId = fixture.FacilitatorId,
            StudentFirstName = "Test",
            StudentLastName = "Student",
            CreatedById = fixture.ManagerId,
        };

        var act = async () => await fixture.Repository.CreateStudentAsync(input);

        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.WithMessage("*filial*");
    }

    [Fact]
    public async Task CreateStudentAsync_WithNonUploaderFacilitator_ThrowsInvalidOperationException()
    {
        await using var fixture = await CreateFixtureAsync();

        var input = new CreateStudentInputDataModel
        {
            ChapterId = 1,
            FacilitatorId = fixture.NonUploaderUserId,
            StudentFirstName = "Test",
            StudentLastName = "Student",
            CreatedById = fixture.ManagerId,
        };

        var act = async () => await fixture.Repository.CreateStudentAsync(input);

        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.WithMessage("*mediador*");
    }

    [Fact]
    public async Task CreateStudentAsync_WithNonExistentFacilitator_ThrowsInvalidOperationException()
    {
        await using var fixture = await CreateFixtureAsync();

        var input = new CreateStudentInputDataModel
        {
            ChapterId = 1,
            FacilitatorId = 99999,
            StudentFirstName = "Test",
            StudentLastName = "Student",
            CreatedById = fixture.ManagerId,
        };

        var act = async () => await fixture.Repository.CreateStudentAsync(input);

        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.WithMessage("*no existe*");
    }

    [Fact]
    public async Task CreateStudentAsync_WithLockedOutFacilitator_ThrowsInvalidOperationException()
    {
        await using var fixture = await CreateFixtureAsync();
        var input = fixture.CreateStudentInput(fixture.LockedFacilitatorId);

        var act = () => fixture.Repository.CreateStudentAsync(input);

        var exception = await act.Should().ThrowAsync<InvalidOperationException>();
        exception.WithMessage("*deshabilitado*");
    }

    [Fact]
    public async Task CreateStudentAsync_ForAnotherManagerChapter_ThrowsInvalidOperationException()
    {
        await using var fixture = await CreateFixtureAsync();
        var input = fixture.CreateStudentInput(
            fixture.OtherChapterFacilitatorId,
            chapterId: 2,
            createdById: fixture.ManagerId);

        var act = () => fixture.Repository.CreateStudentAsync(input);

        var exception = await act.Should().ThrowAsync<InvalidOperationException>();
        exception.WithMessage("*filial del coordinador*");
    }

    [Fact]
    public async Task CreateStudentAsync_ForAnotherAdminChapter_CreatesStudent()
    {
        await using var fixture = await CreateFixtureAsync();
        var input = fixture.CreateStudentInput(
            fixture.OtherChapterFacilitatorId,
            chapterId: 2,
            createdById: fixture.AdminId);

        var result = await fixture.Repository.CreateStudentAsync(input);

        result.Should().Be(1);
    }

    [Fact]
    public async Task CreateStudentAsync_WithNonExistentCreator_ThrowsInvalidOperationException()
    {
        await using var fixture = await CreateFixtureAsync();
        var input = fixture.CreateStudentInput(createdById: 99999);

        var act = () => fixture.Repository.CreateStudentAsync(input);

        var exception = await act.Should().ThrowAsync<InvalidOperationException>();
        exception.WithMessage("*crea el becario no existe*");
    }

    [Fact]
    public async Task UpdateStudentAsync_WithValidFacilitator_UpdatesStudent()
    {
        await using var fixture = await CreateFixtureAsync();
        var studentId = await fixture.CreateStudentAsync();
        var input = fixture.CreateUpdateInput(studentId, fixture.FacilitatorId);

        var result = await fixture.Repository.UpdateStudentAsync(input);

        result.Should().Be(1);
        await using var verifyDb = await fixture.DbContextFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var student = await verifyDb.Set<Student>().SingleAsync(
            student => student.Id == studentId,
            TestContext.Current.CancellationToken);
        student.FirstName.Should().Be("Updated");
        student.FacilitatorId.Should().Be(fixture.FacilitatorId);
    }

    [Theory]
    [InlineData(InvalidFacilitator.Zero)]
    [InlineData(InvalidFacilitator.NonExistent)]
    [InlineData(InvalidFacilitator.OtherChapter)]
    [InlineData(InvalidFacilitator.NonUploader)]
    [InlineData(InvalidFacilitator.LockedOut)]
    public async Task UpdateStudentAsync_WithInvalidFacilitator_ThrowsInvalidOperationException(
        InvalidFacilitator invalidFacilitator)
    {
        await using var fixture = await CreateFixtureAsync();
        var studentId = await fixture.CreateStudentAsync();
        var facilitatorId = invalidFacilitator switch
        {
            InvalidFacilitator.Zero => 0,
            InvalidFacilitator.NonExistent => 99999,
            InvalidFacilitator.OtherChapter => fixture.OtherChapterFacilitatorId,
            InvalidFacilitator.NonUploader => fixture.NonUploaderUserId,
            InvalidFacilitator.LockedOut => fixture.LockedFacilitatorId,
            _ => throw new ArgumentOutOfRangeException(nameof(invalidFacilitator)),
        };
        var input = fixture.CreateUpdateInput(studentId, facilitatorId);

        var act = () => fixture.Repository.UpdateStudentAsync(input);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetActiveStudentDisplayNameInChapterAsync_Returns_FullName_When_Student_Is_Active_In_Chapter()
    {
        await using var fixture = await CreateFixtureAsync();
        var studentId = await fixture.CreateStudentAsync();

        var result = await fixture.Repository.GetActiveStudentDisplayNameInChapterAsync(studentId, chapterId: 1);

        result.Should().Be("Test Student");
    }

    [Fact]
    public async Task GetActiveStudentDisplayNameInChapterAsync_Returns_Null_When_Student_Does_Not_Exist()
    {
        await using var fixture = await CreateFixtureAsync();

        var result = await fixture.Repository.GetActiveStudentDisplayNameInChapterAsync(studentId: 99, chapterId: 1);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetActiveStudentDisplayNameInChapterAsync_Returns_Null_When_Student_Is_In_Another_Chapter()
    {
        await using var fixture = await CreateFixtureAsync();
        var studentId = await fixture.CreateStudentAsync(
            fixture.CreateStudentInput(
                fixture.OtherChapterFacilitatorId,
                chapterId: 2,
                createdById: fixture.AdminId));

        var result = await fixture.Repository.GetActiveStudentDisplayNameInChapterAsync(studentId, chapterId: 1);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetActiveStudentDisplayNameInChapterAsync_Returns_Null_When_Student_Is_Inactive()
    {
        await using var fixture = await CreateFixtureAsync();
        var studentId = await fixture.CreateStudentAsync();
        await fixture.DisableStudentAsync(studentId);

        var result = await fixture.Repository.GetActiveStudentDisplayNameInChapterAsync(studentId, chapterId: 1);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetActiveStudentDisplayNameInChapterAsync_Returns_Null_When_Student_Is_Deleted()
    {
        await using var fixture = await CreateFixtureAsync();
        var studentId = await fixture.CreateStudentAsync();
        await fixture.SetStudentDeletedAsync(studentId);

        var result = await fixture.Repository.GetActiveStudentDisplayNameInChapterAsync(studentId, chapterId: 1);

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateStudentAsync_FromAnotherChapter_ThrowsInvalidOperationException()
    {
        await using var fixture = await CreateFixtureAsync();
        var studentId = await fixture.CreateStudentAsync();
        var input = fixture.CreateUpdateInput(
            studentId,
            fixture.FacilitatorId,
            updatedById: fixture.OtherChapterFacilitatorId);

        var act = () => fixture.Repository.UpdateStudentAsync(input);

        var exception = await act.Should().ThrowAsync<InvalidOperationException>();
        exception.WithMessage("*filial del coordinador*");
    }

    [Fact]
    public async Task GetAllStudentsAsync_Returns_Past_Current_And_Future_Sponsorships()
    {
        await using var fixture = await CreateFixtureAsync();
        var studentId = await fixture.CreateStudentAsync();
        var pastStart = DateTime.UtcNow.AddYears(-2);
        var pastEnd = DateTime.UtcNow.AddMonths(-2);
        var currentStart = DateTime.UtcNow.AddMonths(-1);
        var currentEnd = DateTime.UtcNow.AddMonths(1);
        var futureStart = DateTime.UtcNow.AddMonths(2);
        var futureEnd = DateTime.UtcNow.AddMonths(8);

        await using (var db = await fixture.DbContextFactory.CreateDbContextAsync(TestContext.Current.CancellationToken))
        {
            db.Set<Chapter>().Add(new Chapter
            {
                Id = 1,
                Name = "Córdoba",
                CreatedById = fixture.ManagerId,
            });
            db.Set<Sponsor>().AddRange(
                new Sponsor
                {
                    Id = 201,
                    FirstName = "Carlos",
                    LastName = "Pasado",
                    Email = "carlos@fonbec.test",
                    ChapterId = 1,
                    CreatedById = fixture.ManagerId,
                },
                new Sponsor
                {
                    Id = 202,
                    FirstName = "Elena",
                    LastName = "Actual",
                    Email = "elena@fonbec.test",
                    ChapterId = 1,
                    CreatedById = fixture.ManagerId,
                },
                new Sponsor
                {
                    Id = 203,
                    FirstName = "Mario",
                    LastName = "Futuro",
                    Email = "mario@fonbec.test",
                    ChapterId = 1,
                    CreatedById = fixture.ManagerId,
                });
            db.Set<Sponsorship>().AddRange(
                new Sponsorship
                {
                    StudentId = studentId,
                    SponsorId = 201,
                    StartDate = pastStart,
                    EndDate = pastEnd,
                    CreatedById = fixture.ManagerId,
                },
                new Sponsorship
                {
                    StudentId = studentId,
                    SponsorId = 202,
                    StartDate = currentStart,
                    EndDate = currentEnd,
                    CreatedById = fixture.ManagerId,
                },
                new Sponsorship
                {
                    StudentId = studentId,
                    SponsorId = 203,
                    StartDate = futureStart,
                    EndDate = futureEnd,
                    CreatedById = fixture.ManagerId,
                });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var result = await fixture.Repository.GetAllStudentsAsync(chapterId: 1);

        var student = result.Should().ContainSingle().Which;
        student.ActiveSponsors.Should().HaveCount(3);
        student.ActiveSponsors.Should().ContainSingle(s => s.Name == "Carlos Pasado"
            && s.StartDate == pastStart
            && s.EndDate == pastEnd);
        student.ActiveSponsors.Should().ContainSingle(s => s.Name == "Elena Actual"
            && s.StartDate == currentStart
            && s.EndDate == currentEnd);
        student.ActiveSponsors.Should().ContainSingle(s => s.Name == "Mario Futuro"
            && s.StartDate == futureStart
            && s.EndDate == futureEnd);
    }

    private static async Task<StudentRepositoryFixture> CreateFixtureAsync()
    {
        var services = new ServiceCollection();
        var databaseName = Guid.NewGuid().ToString();

        services.AddDbContextFactory<FonbecWebDbContext>(options =>
            options.UseInMemoryDatabase(databaseName)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

        services.AddIdentityCore<FonbecWebUser>()
            .AddRoles<FonbecWebRole>()
            .AddEntityFrameworkStores<FonbecWebDbContext>();

        var provider = services.BuildServiceProvider();

        var userManager = provider.GetRequiredService<UserManager<FonbecWebUser>>();
        var roleManager = provider.GetRequiredService<RoleManager<FonbecWebRole>>();
        var dbContextFactory = provider.GetRequiredService<IDbContextFactory<FonbecWebDbContext>>();
        var repository = new StudentRepository(dbContextFactory, userManager);

        // Seed roles first — required before AddToRoleAsync
        foreach (var roleName in FonbecRole.AllRoles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var roleResult = await roleManager.CreateAsync(new FonbecWebRole { Name = roleName });
                roleResult.Succeeded.Should().BeTrue();
            }
        }

        var facilitator = await CreateUserAsync(
            userManager, "facilitator@test.com", chapterId: 1, FonbecRole.Uploader);
        var otherChapterFacilitator = await CreateUserAsync(
            userManager, "other-chapter@test.com", chapterId: 2, FonbecRole.Uploader);
        var lockedFacilitator = await CreateUserAsync(
            userManager,
            "locked@test.com",
            chapterId: 1,
            FonbecRole.Uploader,
            lockoutEnd: DateTimeOffset.MaxValue);
        var nonUploader = await CreateUserAsync(
            userManager, "other@test.com", chapterId: 1);
        var manager = await CreateUserAsync(
            userManager, "manager@test.com", chapterId: 1, FonbecRole.Manager);
        var admin = await CreateUserAsync(
            userManager, "admin@test.com", chapterId: null, FonbecRole.Admin);

        return new StudentRepositoryFixture(
            provider,
            repository,
            dbContextFactory,
            facilitator.Id,
            otherChapterFacilitator.Id,
            lockedFacilitator.Id,
            nonUploader.Id,
            manager.Id,
            admin.Id);
    }

    private static async Task<FonbecWebUser> CreateUserAsync(
        UserManager<FonbecWebUser> userManager,
        string email,
        int? chapterId,
        string? role = null,
        DateTimeOffset? lockoutEnd = null)
    {
        var user = new FonbecWebUser
        {
            UserName = email,
            Email = email,
            SecurityStamp = Guid.NewGuid().ToString(),
            FirstName = email,
            LastName = "Test",
            ChapterId = chapterId,
            LockoutEnabled = lockoutEnd.HasValue,
            LockoutEnd = lockoutEnd,
        };

        var createResult = await userManager.CreateAsync(user);
        createResult.Succeeded.Should().BeTrue();

        if (role is not null)
        {
            var roleResult = await userManager.AddToRoleAsync(user, role);
            roleResult.Succeeded.Should().BeTrue();
        }

        return user;
    }

    public enum InvalidFacilitator
    {
        Zero,
        NonExistent,
        OtherChapter,
        NonUploader,
        LockedOut,
    }

    private sealed class StudentRepositoryFixture(
        ServiceProvider provider,
        StudentRepository repository,
        IDbContextFactory<FonbecWebDbContext> dbContextFactory,
        int facilitatorId,
        int otherChapterFacilitatorId,
        int lockedFacilitatorId,
        int nonUploaderUserId,
        int managerId,
        int adminId) : IAsyncDisposable
    {
        public StudentRepository Repository { get; } = repository;

        public IDbContextFactory<FonbecWebDbContext> DbContextFactory { get; } = dbContextFactory;

        public int FacilitatorId { get; } = facilitatorId;

        public int OtherChapterFacilitatorId { get; } = otherChapterFacilitatorId;

        public int LockedFacilitatorId { get; } = lockedFacilitatorId;

        public int NonUploaderUserId { get; } = nonUploaderUserId;

        public int ManagerId { get; } = managerId;

        public int AdminId { get; } = adminId;

        public CreateStudentInputDataModel CreateStudentInput(
            int? facilitatorId = null,
            int chapterId = 1,
            int? createdById = null) =>
            new()
            {
                ChapterId = chapterId,
                FacilitatorId = facilitatorId ?? FacilitatorId,
                StudentFirstName = "Test",
                StudentLastName = "Student",
                CreatedById = createdById ?? ManagerId,
            };

        public Task<int> CreateStudentAsync() => CreateStudentAsync(CreateStudentInput());

        public async Task<int> CreateStudentAsync(CreateStudentInputDataModel input)
        {
            await Repository.CreateStudentAsync(input);

            await using var db = await DbContextFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
            return await db.Set<Student>()
                .Where(student => student.FirstName == input.StudentFirstName
                                  && student.LastName == input.StudentLastName
                                  && student.ChapterId == input.ChapterId)
                .Select(student => student.Id)
                .SingleAsync(TestContext.Current.CancellationToken);
        }

        public async Task DisableStudentAsync(int studentId)
        {
            await using var db = await DbContextFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
            var student = await db.Set<Student>().SingleAsync(
                s => s.Id == studentId,
                TestContext.Current.CancellationToken);
            student.DisabledById = ManagerId;
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        public async Task SetStudentDeletedAsync(int studentId)
        {
            await using var db = await DbContextFactory.CreateDbContextAsync(TestContext.Current.CancellationToken);
            var student = await db.Set<Student>().SingleAsync(
                s => s.Id == studentId,
                TestContext.Current.CancellationToken);
            student.IsDeleted = true;
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        public UpdateStudentInputDataModel CreateUpdateInput(
            int studentId,
            int facilitatorId,
            int? updatedById = null) =>
            new()
            {
                StudentId = studentId,
                StudentFirstName = "Updated",
                StudentLastName = "Student",
                FacilitatorId = facilitatorId,
                UpdatedById = updatedById ?? ManagerId,
            };

        public ValueTask DisposeAsync()
        {
            provider.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}