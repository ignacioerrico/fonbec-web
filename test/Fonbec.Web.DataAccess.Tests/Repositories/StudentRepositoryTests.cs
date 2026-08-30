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
            CreatedById = 1,
        };

        var result = await fixture.Repository.CreateStudentAsync(input);

        result.Should().Be(1);

        await using var verifyDb = await fixture.DbContextFactory.CreateDbContextAsync();
        var student = await verifyDb.Set<Student>().FirstOrDefaultAsync();
        student.Should().NotBeNull();
        student!.ChapterId.Should().Be(1);
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
            CreatedById = 1,
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
            CreatedById = 1,
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
            CreatedById = 1,
        };

        var act = async () => await fixture.Repository.CreateStudentAsync(input);

        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.WithMessage("*no existe*");
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

        var facilitator = new FonbecWebUser
        {
            UserName = "facilitator@test.com",
            NormalizedUserName = "FACILITATOR@TEST.COM",
            Email = "facilitator@test.com",
            NormalizedEmail = "FACILITATOR@TEST.COM",
            SecurityStamp = Guid.NewGuid().ToString(),
            FirstName = "Marina",
            LastName = "Simian",
            ChapterId = 1,
            LockoutEnabled = false,
        };

        var nonUploader = new FonbecWebUser
        {
            UserName = "other@test.com",
            NormalizedUserName = "OTHER@TEST.COM",
            Email = "other@test.com",
            NormalizedEmail = "OTHER@TEST.COM",
            SecurityStamp = Guid.NewGuid().ToString(),
            FirstName = "Other",
            LastName = "User",
            ChapterId = 1,
            LockoutEnabled = false,
        };

        var createResult1 = await userManager.CreateAsync(facilitator);
        createResult1.Succeeded.Should().BeTrue();
        var roleResult1 = await userManager.AddToRoleAsync(facilitator, FonbecRole.Uploader);
        roleResult1.Succeeded.Should().BeTrue();

        var createResult2 = await userManager.CreateAsync(nonUploader);
        createResult2.Succeeded.Should().BeTrue();
        // Intentionally NOT adding to Uploader role

        return new StudentRepositoryFixture(provider, repository, dbContextFactory, facilitator.Id, nonUploader.Id);
    }

    private sealed class StudentRepositoryFixture(
        ServiceProvider provider,
        StudentRepository repository,
        IDbContextFactory<FonbecWebDbContext> dbContextFactory,
        int facilitatorId,
        int nonUploaderUserId) : IAsyncDisposable
    {
        public StudentRepository Repository { get; } = repository;

        public IDbContextFactory<FonbecWebDbContext> DbContextFactory { get; } = dbContextFactory;

        public int FacilitatorId { get; } = facilitatorId;

        public int NonUploaderUserId { get; } = nonUploaderUserId;

        public ValueTask DisposeAsync()
        {
            provider.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}