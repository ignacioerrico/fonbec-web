using FluentAssertions;
using Fonbec.Web.DataAccess.Entities.Enums;
using Fonbec.Web.Logic.Models.Sponsors;

namespace Fonbec.Web.Logic.Tests.Models.Sponsors;

public class SponsorsListViewModelTests
{
    [Fact]
    public void IsIdenticalTo_ReturnsTrue_WhenAllValuesAreEqual_AfterBeingNormalized()
    {
        var thisViewModel = new SponsorsListViewModel
        {
            SponsorFirstName = "First Name",
            SponsorLastName = "Last Name",
            SponsorNickName = "Nick Name",
            SponsorGender = Gender.Female,
            SponsorEmail = "user@mail.com",
            SponsorPhoneNumber = "314-1592",
            SponsorCompanyId = 314,
        };

        var otherViewModel = new SponsorsListViewModel
        {
            SponsorFirstName = "  fiRSt   nAMe  ",
            SponsorLastName = "  laSt  nAme  ",
            SponsorNickName = "  nICK  naMe   ",
            SponsorGender = Gender.Female,
            SponsorEmail = "  uSeR@MAIL.com  ",
            SponsorPhoneNumber = "  314-1592  ",
            SponsorCompanyId = 314,
        };

        var result = thisViewModel.IsIdenticalTo(otherViewModel);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsIdenticalTo_LastNameMismatch_ReturnsFalse()
    {
        var thisViewModel = new SponsorsListViewModel
        {
            SponsorFirstName = "First Name",
            SponsorLastName = "Last Name",
            SponsorNickName = "Nick Name",
            SponsorGender = Gender.Female,
            SponsorEmail = "user@mail.com",
            SponsorPhoneNumber = "314-1592",
            SponsorCompanyId = 314,
        };

        var otherViewModel = new SponsorsListViewModel
        {
            SponsorFirstName = "First Name",
            SponsorLastName = "Doe",
            SponsorNickName = "Nick Name",
            SponsorGender = Gender.Female,
            SponsorEmail = "user@mail.com",
            SponsorPhoneNumber = "314-1592",
            SponsorCompanyId = 314,
        };

        var result = thisViewModel.IsIdenticalTo(otherViewModel);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsIdenticalTo_NickNameMismatch_ReturnsFalse()
    {
        var thisViewModel = new SponsorsListViewModel
        {
            SponsorFirstName = "First Name",
            SponsorLastName = "Last Name",
            SponsorNickName = "Nick Name",
            SponsorGender = Gender.Female,
            SponsorEmail = "user@mail.com",
            SponsorPhoneNumber = "314-1592",
            SponsorCompanyId = 314,
        };

        var otherViewModel = new SponsorsListViewModel
        {
            SponsorFirstName = "First Name",
            SponsorLastName = "Last Name",
            SponsorNickName = "Johnny",
            SponsorGender = Gender.Female,
            SponsorEmail = "user@mail.com",
            SponsorPhoneNumber = "314-1592",
            SponsorCompanyId = 314,
        };

        var result = thisViewModel.IsIdenticalTo(otherViewModel);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsIdenticalTo_GenderMismatch_ReturnsFalse()
    {
        var thisViewModel = new SponsorsListViewModel
        {
            SponsorFirstName = "First Name",
            SponsorLastName = "Last Name",
            SponsorNickName = "Nick Name",
            SponsorGender = Gender.Female,
            SponsorEmail = "user@mail.com",
            SponsorPhoneNumber = "314-1592",
            SponsorCompanyId = 314,
        };

        var otherViewModel = new SponsorsListViewModel
        {
            SponsorFirstName = "First Name",
            SponsorLastName = "Last Name",
            SponsorNickName = "Nick Name",
            SponsorGender = Gender.Male,
            SponsorEmail = "user@mail.com",
            SponsorPhoneNumber = "314-1592",
            SponsorCompanyId = 314,
        };

        var result = thisViewModel.IsIdenticalTo(otherViewModel);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsIdenticalTo_EmailMismatch_ReturnsFalse()
    {
        var thisViewModel = new SponsorsListViewModel
        {
            SponsorFirstName = "First Name",
            SponsorLastName = "Last Name",
            SponsorNickName = "Nick Name",
            SponsorGender = Gender.Female,
            SponsorEmail = "user@mail.com",
            SponsorPhoneNumber = "314-1592",
            SponsorCompanyId = 314,
        };

        var otherViewModel = new SponsorsListViewModel
        {
            SponsorFirstName = "First Name",
            SponsorLastName = "Last Name",
            SponsorNickName = "Nick Name",
            SponsorGender = Gender.Female,
            SponsorEmail = "john@mail.com",
            SponsorPhoneNumber = "314-1592",
            SponsorCompanyId = 314,
        };

        var result = thisViewModel.IsIdenticalTo(otherViewModel);

        result.Should().BeFalse();
    }

   [Fact]
    public void IsIdenticalTo_PhoneNumberMismatch_ReturnsFalse()
    {
        var thisViewModel = new SponsorsListViewModel
        {
            SponsorFirstName = "First Name",
            SponsorLastName = "Last Name",
            SponsorNickName = "Nick Name",
            SponsorGender = Gender.Female,
            SponsorEmail = "user@mail.com",
            SponsorPhoneNumber = "314-1592",
            SponsorCompanyId = 314,
        };

        var otherViewModel = new SponsorsListViewModel
        {
            SponsorFirstName = "First Name",
            SponsorLastName = "Last Name",
            SponsorNickName = "Nick Name",
            SponsorGender = Gender.Female,
            SponsorEmail = "user@mail.com",
            SponsorPhoneNumber = "315-1592",
            SponsorCompanyId = 314,
        };

        var result = thisViewModel.IsIdenticalTo(otherViewModel);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsIdenticalTo_CompanyIdMismatch_ReturnsFalse()
    {
        var thisViewModel = new SponsorsListViewModel
        {
            SponsorFirstName = "First Name",
            SponsorLastName = "Last Name",
            SponsorNickName = "Nick Name",
            SponsorGender = Gender.Female,
            SponsorEmail = "user@mail.com",
            SponsorPhoneNumber = "314-1592",
            SponsorCompanyId = 314,
        };

        var otherViewModel = new SponsorsListViewModel
        {
            SponsorFirstName = "First Name",
            SponsorLastName = "Last Name",
            SponsorNickName = "Nick Name",
            SponsorGender = Gender.Female,
            SponsorEmail = "user@mail.com",
            SponsorPhoneNumber = "315-1592",
            SponsorCompanyId = 315,
        };

        var result = thisViewModel.IsIdenticalTo(otherViewModel);

        result.Should().BeFalse();
    }
}