using FluentAssertions;
using Fonbec.Web.Logic.Util;

namespace Fonbec.Web.Logic.Tests.Util;

public class ContactFieldValidatorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("contacto@empresa.org")]
    public void IsValidEmail_Returns_True_When_Email_Is_Empty_Or_Valid(string? email)
    {
        ContactFieldValidator.IsValidEmail(email).Should().BeTrue();
    }

    [Theory]
    [InlineData("nnnnn@@")]
    [InlineData("contacto@empresa")]
    [InlineData("@empresa.org")]
    public void IsValidEmail_Returns_False_When_Email_Is_Invalid(string email)
    {
        ContactFieldValidator.IsValidEmail(email).Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1234567")]
    [InlineData("+541112345678")]
    [InlineData("(+54 9) 351 1234-4321")]
    [InlineData("123-4567")]
    public void IsValidPhone_Returns_True_When_Phone_Is_Empty_Or_Valid(string? phone)
    {
        ContactFieldValidator.IsValidPhone(phone).Should().BeTrue();
    }

    [Theory]
    [InlineData("123456")]
    [InlineData("2222222222222222")]
    [InlineData("phone1234567")]
    [InlineData("+++++++")]
    public void IsValidPhone_Returns_False_When_Phone_Is_Invalid(string phone)
    {
        ContactFieldValidator.IsValidPhone(phone).Should().BeFalse();
    }
}