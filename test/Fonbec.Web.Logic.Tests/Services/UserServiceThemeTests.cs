using FluentAssertions;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Services;
using NSubstitute;

namespace Fonbec.Web.Logic.Tests.Services;

public class UserServiceThemeTests
{
    [Fact]
    public async Task GetIsDarkModeAsync_ReturnsStoredPreference()
    {
        var repository = Substitute.For<IUserRepository>();
        repository.GetIsDarkModeAsync(7).Returns(true);
        var service = new UserService(repository, null!, null!, []);

        var isDarkMode = await service.GetIsDarkModeAsync(7);

        isDarkMode.Should().BeTrue();
    }

    [Fact]
    public async Task GetIsDarkModeAsync_ReturnsLight_WhenUserHasNoPreference()
    {
        var repository = Substitute.For<IUserRepository>();
        repository.GetIsDarkModeAsync(7).Returns(false);
        var service = new UserService(repository, null!, null!, []);

        var isDarkMode = await service.GetIsDarkModeAsync(7);

        isDarkMode.Should().BeFalse();
    }

    [Fact]
    public async Task SetIsDarkModeAsync_SavesTheCurrentUsersPreference()
    {
        var repository = Substitute.For<IUserRepository>();
        var service = new UserService(repository, null!, null!, []);

        await service.SetIsDarkModeAsync(7, true);

        await repository.Received(1).SetIsDarkModeAsync(7, true);
    }
}