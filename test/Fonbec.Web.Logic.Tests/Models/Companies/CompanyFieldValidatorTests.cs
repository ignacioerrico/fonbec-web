using FluentAssertions;
using Fonbec.Web.Logic.Models.Companies;

namespace Fonbec.Web.Logic.Tests.Models.Companies;

public class CompanyFieldValidatorTests
{
    [Theory]
    [InlineData("Empresa 111")]
    [InlineData("Fundación")]
    public void IsValidName_Returns_True_When_Name_Contains_Letters(string name)
    {
        CompanyFieldValidator.IsValidName(name).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1111111111111")]
    public void IsValidName_Returns_False_When_Name_Is_Missing_Or_Numeric(string? name)
    {
        CompanyFieldValidator.IsValidName(name).Should().BeFalse();
    }
}
