using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Users;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.Models.Users;
using Mapster;
using System.Data;

namespace Fonbec.Web.Logic.Tests.Models.Users;

/// <summary>
/// The tests cover:
/// - All fields mapped directly.
/// - Null handling for UserNickName, UserEmail, and UserPhoneNumber.
/// - Both branches for IsUserActive logic.
/// </summary>
public class AllUsersViewModelMappingDefinitionsTests : MappingTestBase
{
    [Fact]
    public void Maps_User_From_DataModel_To_ViewModel()
    {
        var now = DateTimeOffset.Now;
        var dataModel = new AllUsersDataModel
        {
            UserId = 1,
            UserFirstName = "John",
            UserLastName = "Doe",
            UserNickName = "JD",
            UserGender = Gender.Male,
            UserEmail = "john.doe@example.com",
            UserPhoneNumber = "1234567890",
            IsUserLockedOut = false,
            UserLockOutEndsOnUtc = now.AddMinutes(-1)
        };

        var viewModel = dataModel.Adapt<AllUsersViewModel>(Config);

        viewModel.UserId.Should().Be(1);
        viewModel.UserFirstName.Should().Be("John");
        viewModel.UserLastName.Should().Be("Doe");
        viewModel.UserNickName.Should().Be("JD");
        viewModel.UserGender.Should().Be(Gender.Male);
        viewModel.UserEmail.Should().Be("john.doe@example.com");
        viewModel.UserPhoneNumber.Should().Be("1234567890");
        viewModel.IsUserActive.Should().BeTrue();
    }

    [Fact]
    public void Sets_UserNickName_Empty_When_Null()
    {
        var dataModel = new AllUsersDataModel
        {
            UserNickName = null,
            UserLockOutEndsOnUtc = null
        };

        var viewModel = dataModel.Adapt<AllUsersViewModel>(Config);

        viewModel.UserNickName.Should().BeEmpty();
    }

    [Fact]
    public void Sets_UserEmail_Empty_When_Null()
    {
        var dataModel = new AllUsersDataModel
        {
            UserEmail = null,
            UserLockOutEndsOnUtc = null
        };

        var viewModel = dataModel.Adapt<AllUsersViewModel>(Config);

        viewModel.UserEmail.Should().BeEmpty();
    }

    [Fact]
    public void Sets_UserPhoneNumber_Empty_When_Null()
    {
        var dataModel = new AllUsersDataModel
        {
            UserPhoneNumber = null,
            UserLockOutEndsOnUtc = null
        };

        var viewModel = dataModel.Adapt<AllUsersViewModel>(Config);

        viewModel.UserPhoneNumber.Should().BeEmpty();
    }

    [Fact]
    public void IsUserActive_True_When_NotLockedOut_And_LockoutEndsInPast()
    {
        var dataModel = new AllUsersDataModel
        {
            IsUserLockedOut = false,
            UserLockOutEndsOnUtc = DateTimeOffset.Now.AddMinutes(-5)
        };

        var viewModel = dataModel.Adapt<AllUsersViewModel>(Config);

        viewModel.IsUserActive.Should().BeTrue();
    }

    [Fact]
    public void IsUserActive_False_When_LockedOut()
    {
        var dataModel = new AllUsersDataModel
        {
            IsUserLockedOut = true,
            UserLockOutEndsOnUtc = DateTimeOffset.Now.AddMinutes(-5)
        };

        var viewModel = dataModel.Adapt<AllUsersViewModel>(Config);

        viewModel.IsUserActive.Should().BeFalse();
    }

    [Fact]
    public void IsUserActive_True_When_LockoutEndsOnUtc_Is_Null_And_NotLockedOut()
    {
        var dataModel = new AllUsersDataModel
        {
            IsUserLockedOut = false,
            UserLockOutEndsOnUtc = null
        };

        var viewModel = dataModel.Adapt<AllUsersViewModel>(Config);

        viewModel.IsUserActive.Should().BeTrue();
    }

    [Fact]
    public void IsUserActive_False_When_LockoutEndsOnUtc_Is_Null_And_LockedOut()
    {
        var dataModel = new AllUsersDataModel
        {
            IsUserLockedOut = true,
            UserLockOutEndsOnUtc = null
        };

        var viewModel = dataModel.Adapt<AllUsersViewModel>(Config);

        viewModel.IsUserActive.Should().BeFalse();
    }
}