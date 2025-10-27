using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Users;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.Models.Users;
using Mapster;

namespace Fonbec.Web.Logic.Tests.Models.Users;

/// <summary>
/// The tests cover:
/// - All fields mapped directly.
/// - Null handling for UserNickName, UserEmail, and UserPhoneNumber.
/// - IsUserActive logic as per mapping definition.
/// - Roles property default value.
/// </summary>
public class UsersListViewModelMappingDefinitionsTests : MappingTestBase
{
    [Fact]
    public void Maps_User_From_UserDataModel_To_ViewModel()
    {
        var now = DateTimeOffset.Now;
        var userDataModel = new AllUsersUserDataModel
        {
            UserId = 1,
            UserFirstName = "John",
            UserLastName = "Doe",
            UserNickName = "JD",
            UserGender = Gender.Male,
            UserEmail = "john.doe@example.com",
            UserPhoneNumber = "1234567890",
            UserNotes = "Some personal notes",
            CanUserBeLockedOut = false,
            UserLockOutEndsOnUtc = now.AddMinutes(-1)
        };

        var viewModel = userDataModel.Adapt<UsersListViewModel>(Config);

        viewModel.UserId.Should().Be(1);
        viewModel.UserFirstName.Should().Be("John");
        viewModel.UserLastName.Should().Be("Doe");
        viewModel.UserNickName.Should().Be("JD");
        viewModel.UserGender.Should().Be(Gender.Male);
        viewModel.UserEmail.Should().Be("john.doe@example.com");
        viewModel.UserPhoneNumber.Should().Be("1234567890");
        viewModel.UserNotes.Should().Be("Some personal notes");
        viewModel.IsUserActive.Should().BeTrue();
        viewModel.UserRole.Should().BeNull(); // Not mapped by default
    }

    [Fact]
    public void Sets_UserNickName_Empty_When_Null()
    {
        var userDataModel = new AllUsersUserDataModel
        {
            UserNickName = null,
            UserLockOutEndsOnUtc = null
        };

        var viewModel = userDataModel.Adapt<UsersListViewModel>(Config);

        viewModel.UserNickName.Should().BeEmpty();
    }

    [Fact]
    public void Sets_UserEmail_Empty_When_Null()
    {
        var userDataModel = new AllUsersUserDataModel
        {
            UserEmail = null,
            UserLockOutEndsOnUtc = null
        };

        var viewModel = userDataModel.Adapt<UsersListViewModel>(Config);

        viewModel.UserEmail.Should().BeEmpty();
    }

    [Fact]
    public void Sets_UserPhoneNumber_Empty_When_Null()
    {
        var userDataModel = new AllUsersUserDataModel
        {
            UserPhoneNumber = null,
            UserLockOutEndsOnUtc = null
        };

        var viewModel = userDataModel.Adapt<UsersListViewModel>(Config);

        viewModel.UserPhoneNumber.Should().BeEmpty();
    }

    [Fact]
    public void Maps_Null_DataModel_UserNotes_To_EmptyString()
    {
        var userDataModel = new AllUsersUserDataModel
        {
            UserNotes = null
        };

        var viewModel = userDataModel.Adapt<UsersListViewModel>(Config);

        viewModel.UserNotes.Should().BeEmpty();
    }

    [Fact]
    public void IsUserActive_True_When_CannotBeLockedOut()
    {
        var userDataModel = new AllUsersUserDataModel
        {
            CanUserBeLockedOut = false,
            UserLockOutEndsOnUtc = DateTimeOffset.Now.AddMinutes(10)
        };

        var viewModel = userDataModel.Adapt<UsersListViewModel>(Config);

        viewModel.IsUserActive.Should().BeTrue();
    }

    [Fact]
    public void IsUserActive_True_When_LockoutEndsOnUtc_Is_Null()
    {
        var userDataModel = new AllUsersUserDataModel
        {
            CanUserBeLockedOut = true,
            UserLockOutEndsOnUtc = null
        };

        var viewModel = userDataModel.Adapt<UsersListViewModel>(Config);

        viewModel.IsUserActive.Should().BeTrue();
    }

    [Fact]
    public void IsUserActive_True_When_LockoutEndsOnUtc_Is_In_The_Past()
    {
        var userDataModel = new AllUsersUserDataModel
        {
            CanUserBeLockedOut = true,
            UserLockOutEndsOnUtc = DateTimeOffset.Now.AddMinutes(-5)
        };

        var viewModel = userDataModel.Adapt<UsersListViewModel>(Config);

        viewModel.IsUserActive.Should().BeTrue();
    }

    [Fact]
    public void IsUserActive_False_When_CanBeLockedOut_And_LockoutEndsOnUtc_Is_In_The_Future()
    {
        var userDataModel = new AllUsersUserDataModel
        {
            CanUserBeLockedOut = true,
            UserLockOutEndsOnUtc = DateTimeOffset.Now.AddMinutes(5)
        };

        var viewModel = userDataModel.Adapt<UsersListViewModel>(Config);

        viewModel.IsUserActive.Should().BeFalse();
    }
}