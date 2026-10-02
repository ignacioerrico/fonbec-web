using FluentAssertions;
using Fonbec.Web.Logic.Models.Sponsors;

namespace Fonbec.Web.Logic.Tests.Models.Sponsors;

public class SendAlsoToValidatorTests
{
    [Fact]
    public void Empty_List_Is_Valid()
    {
        var errors = SendAlsoToValidator.Validate([], "ana@ejemplo.com");

        errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("", "ana@x.com")]
    [InlineData("   ", "ana@x.com")]
    [InlineData("Ana Perez", "")]
    [InlineData("Ana Perez", "   ")]
    public void Required_Fields_Are_Rejected(string name, string email)
    {
        var errors = SendAlsoToValidator.Validate(
            [new SendAlsoToValidationItem(name, email)],
            "sponsor@ejemplo.com");

        errors.Should().ContainSingle().Which.Should().Be(SendAlsoToValidator.RequiredMessage);
    }

    [Fact]
    public void Invalid_Email_Is_Rejected()
    {
        var errors = SendAlsoToValidator.Validate(
            [new SendAlsoToValidationItem("Ana Perez", "no-es-un-correo")],
            "sponsor@ejemplo.com");

        errors.Should().Contain(SendAlsoToValidator.InvalidEmailMessage);
    }

    [Fact]
    public void Duplicate_Emails_Are_Rejected_After_Normalization()
    {
        var errors = SendAlsoToValidator.Validate(
            [
                new SendAlsoToValidationItem("Luis Perez", "  Luis@Ejemplo.com "),
                new SendAlsoToValidationItem("Ana Gomez", "luis@ejemplo.com"),
            ],
            "sponsor@ejemplo.com");

        errors.Should().Contain(SendAlsoToValidator.DuplicateMessage);
    }

    [Fact]
    public void Email_Matching_Primary_Is_Rejected_After_Normalization()
    {
        var errors = SendAlsoToValidator.Validate(
            [new SendAlsoToValidationItem("Luis Perez", "  ANA@ejemplo.COM ")],
            " ana@ejemplo.com ");

        errors.Should().Contain(SendAlsoToValidator.MatchesPrimaryMessage);
    }

    [Fact]
    public void ValidateEmailField_Reports_Duplicate_Against_Other_Rows_Only()
    {
        var message = SendAlsoToValidator.ValidateEmailField(
            "luis@ejemplo.com",
            "ana@ejemplo.com",
            ["  LUIS@ejemplo.com  "]);

        message.Should().Be(SendAlsoToValidator.DuplicateMessage);
    }
}