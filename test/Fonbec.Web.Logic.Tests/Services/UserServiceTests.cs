using System.Security.Claims;
using FluentAssertions;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Authorization;
using Fonbec.Web.Logic.Constants;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Logic.Util;
using NSubstitute;

namespace Fonbec.Web.Logic.Tests.Services;

public class UserServiceTests
{
    private static List<PageAccessInfo> DummyPages =>
    [
        new("PageA", "Page A", ["Role1"]),
        new("PageB", "Page B", ["Role1"]),
        new("PageC", "Page C", ["Role2"])
    ];

    [Fact]
    public void GetFonbecAuthClaim_ReturnsClaimValue_WhenClaimExists()
    {
        // Arrange
        const string claimValue = "PageA,PageB";
        var claimsPrincipal = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(FonbecAuth.ClaimType, claimValue)])
        );
        var userService = new UserService(null!, null!, null!, DummyPages);

        // Act
        var result = userService.GetFonbecAuthClaim(claimsPrincipal);

        // Assert
        result.Should().Be(claimValue);
    }

    [Fact]
    public void GetFonbecAuthClaim_ReturnsNull_WhenClaimDoesNotExist()
    {
        // Arrange
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity());
        var userService = new UserService(null!, null!, null!, DummyPages);

        // Act
        var result = userService.GetFonbecAuthClaim(claimsPrincipal);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData("PageA,PageB,PageC", "PageB", true)]
    [InlineData("PageA,PageB,PageC", "PageD", false)]
    [InlineData("PageA , PageB , PageC", "PageB", true)]
    [InlineData("", "PageA", false)]
    public void HasPermission_ReturnsExpectedResult(string claimValue, string page, bool expected)
    {
        // Arrange
        var userService = new UserService(null!, null!, null!, DummyPages);

        // Act
        var result = userService.HasPermission(claimValue, page);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public async Task SetFonbecAuthClaim_OrdersPagesAndCallsSetUserClaim()
    {
        // Arrange
        const int userId = 42;
        var pages = new[] { "PageB", "PageA" };
        var userRepo = Substitute.For<IUserRepository>();
        var userService = new UserService(userRepo, null!, null!, DummyPages);

        // Act
        await userService.SetFonbecAuthClaim(userId, pages);

        // Assert
        await userRepo.Received(1).SetUserClaim(
            userId.ToString(),
            FonbecAuth.ClaimType,
            "PageA,PageB"
        );
    }

    [Fact]
    public async Task GetUserClaim_DelegatesToRepository()
    {
        // Arrange
        const int userId = 1;
        const string claimType = "TestClaim";
        const string expectedValue = "foo";
        var userRepo = Substitute.For<IUserRepository>();
        userRepo.GetUserClaim(userId.ToString(), claimType).Returns(expectedValue);
        var userService = new UserService(userRepo, null!, null!, DummyPages);

        // Act
        var result = await userService.GetUserClaim(userId, claimType);

        // Assert
        result.Should().Be(expectedValue);
    }

    [Fact]
    public async Task SetUserClaim_DelegatesToRepository()
    {
        // Arrange
        const int userId = 1;
        const string claimType = "TestClaim";
        const string claimValue = "bar";
        var userRepo = Substitute.For<IUserRepository>();
        var userService = new UserService(userRepo, null!, null!, DummyPages);

        // Act
        await userService.SetUserClaim(userId, claimType, claimValue);

        // Assert
        await userRepo.Received(1).SetUserClaim(userId.ToString(), claimType, claimValue);
    }

    [Fact]
    public async Task CreateUserAsync_SetsCorrectClaims_ForRole()
    {
        // Arrange
        var userRepo = Substitute.For<IUserRepository>();
        var passwordGen = Substitute.For<IPasswordGeneratorWrapper>();
        var emailSender = Substitute.For<IEmailMessageSender>();
        var userService = new UserService(userRepo, passwordGen, emailSender, DummyPages);

        var model = new Fonbec.Web.Logic.Models.Users.Input.CreateUserInputModel(
            UserChapterId: 1,
            UserFirstName: "John",
            UserLastName: "Doe",
            UserNickName: "JD",
            UserGender: Fonbec.Web.DataAccess.Entities.Enums.Gender.Male,
            UserEmail: "john@doe.com",
            UserPhoneNumber: "1234567890",
            UserRole: "Role1",
            CreatedById: 99
        );

        passwordGen.GeneratePassword().Returns("TestPassword");
        userRepo.CreateUserAsync(Arg.Any<Fonbec.Web.DataAccess.DataModels.Users.Input.CreateUserInputDataModel>()).Returns((42, new List<string>()));

        // Act
        await userService.CreateUserAsync(model);

        // Assert
        await userRepo.Received(1).SetUserClaim(
            "42",
            FonbecAuth.ClaimType,
            "PageA,PageB"
        );
    }
}