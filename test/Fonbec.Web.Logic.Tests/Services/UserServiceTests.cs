using System.Security.Claims;
using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Users.Input;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.DataAccess.Repositories;
using Fonbec.Web.Logic.Authorization;
using Fonbec.Web.Logic.Constants;
using Fonbec.Web.Logic.Services;
using Fonbec.Web.Logic.Util;
using NSubstitute;

namespace Fonbec.Web.Logic.Tests.Services;

public class UserServiceTests
{
    private const string Role1 = "Role1";
    private const string Role2 = "Role2";

    private static List<PageAccessInfo> DummyPages =>
    [
        new("PageA", "Page A", [Role1]),
        new("PageB", "Page B", [Role1]),
        new("PageC", "Page C", [Role2])
    ];

    [Fact]
    public void GetFonbecAuthClaim_ReturnsClaimValue_WhenClaimExists()
    {
        const string claimValue = "PageB";
        var claimsPrincipal = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(FonbecAuth.ClaimType, claimValue)])
        );
        var userService = new UserService(null!, null!, null!, DummyPages);

        var result = userService.GetFonbecAuthClaim(claimsPrincipal);

        result.Should().Be(claimValue);
    }

    [Fact]
    public void GetFonbecAuthClaim_ReturnsNull_WhenClaimDoesNotExist()
    {
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity());
        var userService = new UserService(null!, null!, null!, DummyPages);

        var result = userService.GetFonbecAuthClaim(claimsPrincipal);

        result.Should().BeNull();
    }

    [Theory]
    [InlineData(null, Role1, "PageA", true)]
    [InlineData("", Role1, "PageA", true)]
    [InlineData("PageB", Role1, "PageA", true)]
    [InlineData("PageA", Role1, "PageA", false)]
    [InlineData("PageA,PageB", Role1, "PageB", false)]
    [InlineData(" PageA", Role1, "PageA", false)]
    [InlineData(null, Role1, "PageC", false)]
    [InlineData(null, Role2, "PageC", true)]
    [InlineData("PageC", Role1, "PageA", true)]
    public void HasPermission_ReturnsExpectedResult(string? claimValue, string userRole, string page, bool expected)
    {
        var userService = new UserService(null!, null!, null!, DummyPages);

        var result = userService.HasPermission(claimValue, userRole, page);

        result.Should().Be(expected);
    }

    [Fact]
    public void HasPermission_ReturnsFalse_ForUnknownPage()
    {
        var userService = new UserService(null!, null!, null!, DummyPages);

        userService.HasPermission(null, Role1, "UnknownPage").Should().BeFalse();
    }

    [Fact]
    public async Task GetFonbecAuthClaim_ByUserId_DelegatesToRepository()
    {
        const int userId = 7;
        var userRepo = Substitute.For<IUserRepository>();
        userRepo.GetUserClaim("7", FonbecAuth.ClaimType).Returns("PageA");
        var userService = new UserService(userRepo, null!, null!, DummyPages);

        var result = await userService.GetFonbecAuthClaim(userId);

        result.Should().Be("PageA");
    }

    [Fact]
    public async Task GetUserClaim_ReturnsEmptyString_WhenRepositoryReturnsNull()
    {
        var userRepo = Substitute.For<IUserRepository>();
        userRepo.GetUserClaim("1", FonbecAuth.ClaimType).Returns((string?)null);
        var userService = new UserService(userRepo, null!, null!, DummyPages);

        var result = await userService.GetUserClaim(1, FonbecAuth.ClaimType);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateUserAsync_DoesNotSetFonbecAuthClaim_WhenCreationFails()
    {
        var userRepo = Substitute.For<IUserRepository>();
        var passwordGen = Substitute.For<IPasswordGeneratorWrapper>();
        var emailSender = Substitute.For<IEmailMessageSender>();
        var userService = new UserService(userRepo, passwordGen, emailSender, DummyPages);

        var model = new Fonbec.Web.Logic.Models.Users.Input.CreateUserInputModel(
            UserChapterId: 1,
            UserFirstName: "John",
            UserLastName: "Doe",
            UserNickName: "JD",
            UserGender: Gender.Male,
            UserEmail: "john@doe.com",
            UserPhoneNumber: "1234567890",
            UserNotes: "New user notes",
            UserRole: Role1,
            CreatedById: 99
        );

        passwordGen.GeneratePassword().Returns("TestPassword");
        userRepo.CreateUserAsync(Arg.Any<CreateUserInputDataModel>()).Returns((0, new List<string>()));

        await userService.CreateUserAsync(model);

        await userRepo.DidNotReceive().SetUserClaim(
            Arg.Any<string>(),
            FonbecAuth.ClaimType,
            Arg.Any<string>());
    }

    [Fact]
    public void HasPermission_UsesRoleDefault_WhenPageIsAllowedForMultipleRoles()
    {
        var pages = new List<PageAccessInfo>
        {
            new("SharedPage", "Shared", [Role1, Role2]),
        };
        var userService = new UserService(null!, null!, null!, pages);

        userService.HasPermission(null, Role2, "SharedPage").Should().BeTrue();
        userService.HasPermission("SharedPage", Role2, "SharedPage").Should().BeFalse();
    }

    [Fact]
    public async Task SetFonbecAuthClaim_StoresDeniedPages()
    {
        const int userId = 42;
        var pages = new[] { "PageB", "PageA" };
        var userRepo = Substitute.For<IUserRepository>();
        var userService = new UserService(userRepo, null!, null!, DummyPages);

        await userService.SetFonbecAuthClaim(userId, pages);

        await userRepo.Received(1).SetUserClaim(
            userId.ToString(),
            FonbecAuth.ClaimType,
            "PageA,PageB"
        );
    }

    [Fact]
    public async Task SetFonbecAuthClaim_RemovesClaim_WhenNoDeniedPages()
    {
        const int userId = 42;
        var userRepo = Substitute.For<IUserRepository>();
        var userService = new UserService(userRepo, null!, null!, DummyPages);

        await userService.SetFonbecAuthClaim(userId, []);

        await userRepo.Received(1).RemoveUserClaim(userId.ToString(), FonbecAuth.ClaimType);
        await userRepo.DidNotReceive().SetUserClaim(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task GetUserClaim_DelegatesToRepository()
    {
        const int userId = 1;
        const string claimType = "TestClaim";
        const string expectedValue = "foo";
        var userRepo = Substitute.For<IUserRepository>();
        userRepo.GetUserClaim(userId.ToString(), claimType).Returns(expectedValue);
        var userService = new UserService(userRepo, null!, null!, DummyPages);

        var result = await userService.GetUserClaim(userId, claimType);

        result.Should().Be(expectedValue);
    }

    [Fact]
    public async Task SetUserClaim_DelegatesToRepository()
    {
        const int userId = 1;
        const string claimType = "TestClaim";
        const string claimValue = "bar";
        var userRepo = Substitute.For<IUserRepository>();
        var userService = new UserService(userRepo, null!, null!, DummyPages);

        await userService.SetUserClaim(userId, claimType, claimValue);

        await userRepo.Received(1).SetUserClaim(userId.ToString(), claimType, claimValue);
    }

    [Fact]
    public async Task CreateUserAsync_DoesNotSetFonbecAuthClaim()
    {
        var userRepo = Substitute.For<IUserRepository>();
        var passwordGen = Substitute.For<IPasswordGeneratorWrapper>();
        var emailSender = Substitute.For<IEmailMessageSender>();
        var userService = new UserService(userRepo, passwordGen, emailSender, DummyPages);

        var model = new Fonbec.Web.Logic.Models.Users.Input.CreateUserInputModel(
            UserChapterId: 1,
            UserFirstName: "John",
            UserLastName: "Doe",
            UserNickName: "JD",
            UserGender: Gender.Male,
            UserEmail: "john@doe.com",
            UserPhoneNumber: "1234567890",
            UserNotes: "New user notes",
            UserRole: Role1,
            CreatedById: 99
        );

        passwordGen.GeneratePassword().Returns("TestPassword");
        userRepo.CreateUserAsync(Arg.Any<CreateUserInputDataModel>()).Returns((42, new List<string>()));

        await userService.CreateUserAsync(model);

        await userRepo.DidNotReceive().SetUserClaim(
            Arg.Any<string>(),
            FonbecAuth.ClaimType,
            Arg.Any<string>());
        await userRepo.DidNotReceive().RemoveUserClaim(
            Arg.Any<string>(),
            FonbecAuth.ClaimType);
    }
}