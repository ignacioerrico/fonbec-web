using FluentAssertions;
using Fonbec.Web.DataAccess.Constants;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Fonbec.Web.DataAccess.Tests.Repositories;

public class UserRepositorySelectionTests
{
    [Fact]
    public async Task GetAllUsersInRoleForSelectionAsync_WithChapter_ReturnsOnlyUsersFromThatChapter()
    {
        var databaseName = Guid.NewGuid().ToString();

        var services = new ServiceCollection();
        services.AddDbContext<FonbecWebDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));
        services.AddIdentityCore<FonbecWebUser>()
            .AddRoles<FonbecWebRole>()
            .AddEntityFrameworkStores<FonbecWebDbContext>();

        using var provider = services.BuildServiceProvider();
        var userManager = provider.GetRequiredService<UserManager<FonbecWebUser>>();
        var roleManager = provider.GetRequiredService<RoleManager<FonbecWebRole>>();
        var userStore = provider.GetRequiredService<IUserStore<FonbecWebUser>>();
        var repository = new UserRepository(userManager, userStore, new TestDbContextFactory(databaseName));

        (await roleManager.CreateAsync(new FonbecWebRole { Name = FonbecRole.Uploader }))
            .Succeeded.Should().BeTrue();
        var chapterOneFacilitator = await CreateFacilitatorAsync(
            userManager, "chapter-one@test.com", chapterId: 1);
        await CreateFacilitatorAsync(userManager, "chapter-two@test.com", chapterId: 2);

        var result = (await repository.GetAllUsersInRoleForSelectionAsync(
            FonbecRole.Uploader, chapterId: 1)).ToList();

        result.Should().ContainSingle()
            .Which.Key.Should().Be(chapterOneFacilitator.Id);
    }

    [Fact]
    public async Task CountUsersInRoleAsync_ExcludesLockedOutUsers()
    {
        var databaseName = Guid.NewGuid().ToString();

        var services = new ServiceCollection();
        services.AddDbContext<FonbecWebDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));
        services.AddIdentityCore<FonbecWebUser>()
            .AddRoles<FonbecWebRole>()
            .AddEntityFrameworkStores<FonbecWebDbContext>();

        using var provider = services.BuildServiceProvider();
        var userManager = provider.GetRequiredService<UserManager<FonbecWebUser>>();
        var roleManager = provider.GetRequiredService<RoleManager<FonbecWebRole>>();
        var userStore = provider.GetRequiredService<IUserStore<FonbecWebUser>>();
        var repository = new UserRepository(userManager, userStore, new TestDbContextFactory(databaseName));

        (await roleManager.CreateAsync(new FonbecWebRole { Name = FonbecRole.Uploader }))
            .Succeeded.Should().BeTrue();
        (await roleManager.CreateAsync(new FonbecWebRole { Name = FonbecRole.Reviewer }))
            .Succeeded.Should().BeTrue();

        await CreateFacilitatorAsync(userManager, "active@test.com", chapterId: 1);
        var disabled = await CreateFacilitatorAsync(userManager, "disabled@test.com", chapterId: 1);
        (await userManager.SetLockoutEnabledAsync(disabled, true)).Succeeded.Should().BeTrue();
        (await userManager.SetLockoutEndDateAsync(disabled, DateTimeOffset.MaxValue)).Succeeded.Should().BeTrue();
        await CreateUserInRoleAsync(userManager, "reviewer@test.com", chapterId: 1, FonbecRole.Reviewer);

        var mediators = await repository.CountUsersInRoleAsync(FonbecRole.Uploader, chapterId: 1);

        mediators.Should().Be(1);
    }

    private static Task<FonbecWebUser> CreateFacilitatorAsync(
        UserManager<FonbecWebUser> userManager,
        string email,
        int chapterId) =>
        CreateUserInRoleAsync(userManager, email, chapterId, FonbecRole.Uploader);

    private static async Task<FonbecWebUser> CreateUserInRoleAsync(
        UserManager<FonbecWebUser> userManager,
        string email,
        int chapterId,
        string role)
    {
        var user = new FonbecWebUser
        {
            UserName = email,
            Email = email,
            FirstName = email,
            LastName = "Test",
            ChapterId = chapterId,
        };

        (await userManager.CreateAsync(user)).Succeeded.Should().BeTrue();
        (await userManager.AddToRoleAsync(user, role)).Succeeded.Should().BeTrue();
        return user;
    }

    private sealed class TestDbContextFactory(string databaseName) : IDbContextFactory<FonbecWebDbContext>
    {
        public FonbecWebDbContext CreateDbContext() =>
            new(new DbContextOptionsBuilder<FonbecWebDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options);

        public Task<FonbecWebDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());
    }
}
