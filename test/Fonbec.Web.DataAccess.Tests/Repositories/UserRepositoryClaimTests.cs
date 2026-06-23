using FluentAssertions;
using Fonbec.Web.DataAccess.Entities;
using Fonbec.Web.DataAccess.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Fonbec.Web.DataAccess.Tests.Repositories;

public class UserRepositoryClaimTests
{
    private const string FonbecAuthClaimType = "FonbecAuth";

    [Fact]
    public async Task GetUserClaim_ReturnsNull_WhenUserDoesNotExist()
    {
        await using var fixture = await CreateFixtureAsync();

        var result = await fixture.Repository.GetUserClaim("999", FonbecAuthClaimType);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserClaim_ReturnsNull_WhenClaimDoesNotExist()
    {
        await using var fixture = await CreateFixtureAsync();

        var result = await fixture.Repository.GetUserClaim(fixture.UserId, FonbecAuthClaimType);

        result.Should().BeNull();
    }

    [Fact]
    public async Task SetUserClaim_AddsClaim_WhenClaimDoesNotExist()
    {
        await using var fixture = await CreateFixtureAsync();

        await fixture.Repository.SetUserClaim(fixture.UserId, FonbecAuthClaimType, "PageA");

        var result = await fixture.Repository.GetUserClaim(fixture.UserId, FonbecAuthClaimType);

        result.Should().Be("PageA");
    }

    [Fact]
    public async Task SetUserClaim_ReplacesClaim_WhenClaimAlreadyExists()
    {
        await using var fixture = await CreateFixtureAsync();

        await fixture.Repository.SetUserClaim(fixture.UserId, FonbecAuthClaimType, "PageA");
        await fixture.Repository.SetUserClaim(fixture.UserId, FonbecAuthClaimType, "PageB,PageC");

        var result = await fixture.Repository.GetUserClaim(fixture.UserId, FonbecAuthClaimType);

        result.Should().Be("PageB,PageC");
    }

    [Fact]
    public async Task RemoveUserClaim_RemovesExistingClaim()
    {
        await using var fixture = await CreateFixtureAsync();

        await fixture.Repository.SetUserClaim(fixture.UserId, FonbecAuthClaimType, "PageA");
        await fixture.Repository.RemoveUserClaim(fixture.UserId, FonbecAuthClaimType);

        var result = await fixture.Repository.GetUserClaim(fixture.UserId, FonbecAuthClaimType);

        result.Should().BeNull();
    }

    [Fact]
    public async Task RemoveUserClaim_DoesNothing_WhenClaimDoesNotExist()
    {
        await using var fixture = await CreateFixtureAsync();

        await fixture.Repository.RemoveUserClaim(fixture.UserId, FonbecAuthClaimType);

        var result = await fixture.Repository.GetUserClaim(fixture.UserId, FonbecAuthClaimType);

        result.Should().BeNull();
    }

    [Fact]
    public async Task SetUserClaim_DoesNothing_WhenUserDoesNotExist()
    {
        await using var fixture = await CreateFixtureAsync();

        await fixture.Repository.SetUserClaim("999", FonbecAuthClaimType, "PageA");

        var result = await fixture.Repository.GetUserClaim("999", FonbecAuthClaimType);

        result.Should().BeNull();
    }

    private static async Task<UserRepositoryFixture> CreateFixtureAsync()
    {
        var services = new ServiceCollection();
        var databaseName = Guid.NewGuid().ToString();

        services.AddDbContext<FonbecWebDbContext>(options =>
            options.UseInMemoryDatabase(databaseName)
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

        services.AddIdentityCore<FonbecWebUser>()
            .AddRoles<FonbecWebRole>()
            .AddEntityFrameworkStores<FonbecWebDbContext>();

        var provider = services.BuildServiceProvider();

        var userManager = provider.GetRequiredService<UserManager<FonbecWebUser>>();
        var userStore = provider.GetRequiredService<IUserStore<FonbecWebUser>>();
        var repository = new UserRepository(userManager, userStore);

        var user = new FonbecWebUser
        {
            UserName = "user@test.com",
            NormalizedUserName = "USER@TEST.COM",
            Email = "user@test.com",
            NormalizedEmail = "USER@TEST.COM",
            SecurityStamp = Guid.NewGuid().ToString(),
        };

        var createResult = await userManager.CreateAsync(user);
        createResult.Succeeded.Should().BeTrue();

        return new UserRepositoryFixture(provider, repository, user.Id.ToString());
    }

    private sealed class UserRepositoryFixture(ServiceProvider provider, UserRepository repository, string userId)
        : IAsyncDisposable
    {
        public UserRepository Repository { get; } = repository;

        public string UserId { get; } = userId;

        public ValueTask DisposeAsync()
        {
            provider.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
