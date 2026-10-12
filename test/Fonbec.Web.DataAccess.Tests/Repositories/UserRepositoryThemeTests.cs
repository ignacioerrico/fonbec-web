using FluentAssertions;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Fonbec.Web.DataAccess.Tests.Repositories;

public class UserRepositoryThemeTests
{
    [Fact]
    public async Task IsDarkMode_DefaultsToLight_AndRoundTrips()
    {
        var (repository, userManager) = CreateRepository();
        var user = await CreateUserAsync(userManager, "theme@test.com");

        (await repository.GetIsDarkModeAsync(user.Id)).Should().BeFalse();

        await repository.SetIsDarkModeAsync(user.Id, true);
        (await repository.GetIsDarkModeAsync(user.Id)).Should().BeTrue();

        await repository.SetIsDarkModeAsync(user.Id, false);
        (await repository.GetIsDarkModeAsync(user.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task GetIsDarkModeAsync_UnknownUser_ReturnsLight()
    {
        var (repository, _) = CreateRepository();

        (await repository.GetIsDarkModeAsync(999)).Should().BeFalse();
    }

    [Fact]
    public async Task SetIsDarkModeAsync_UnknownUser_DoesNotThrow()
    {
        var (repository, _) = CreateRepository();

        var act = async () => await repository.SetIsDarkModeAsync(999, true);

        await act.Should().NotThrowAsync();
    }

    private static (UserRepository Repository, UserManager<FonbecWebUser> UserManager) CreateRepository()
    {
        var databaseName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<FonbecWebDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));
        services.AddIdentityCore<FonbecWebUser>()
            .AddRoles<FonbecWebRole>()
            .AddEntityFrameworkStores<FonbecWebDbContext>();

        var provider = services.BuildServiceProvider();
        var userManager = provider.GetRequiredService<UserManager<FonbecWebUser>>();
        var userStore = provider.GetRequiredService<IUserStore<FonbecWebUser>>();
        var repository = new UserRepository(userManager, userStore, new TestDbContextFactory(databaseName));
        return (repository, userManager);
    }

    private static async Task<FonbecWebUser> CreateUserAsync(UserManager<FonbecWebUser> userManager, string email)
    {
        var user = new FonbecWebUser
        {
            UserName = email,
            Email = email,
            FirstName = "Tema",
            LastName = "Prueba",
        };

        (await userManager.CreateAsync(user, "Password1!")).Succeeded.Should().BeTrue();
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