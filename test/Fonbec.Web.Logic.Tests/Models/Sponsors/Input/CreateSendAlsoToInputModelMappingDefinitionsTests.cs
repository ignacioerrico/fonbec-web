using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Sponsors.Input;
using Fonbec.Web.Logic.Models.Sponsors.Input;
using Mapster;

namespace Fonbec.Web.Logic.Tests.Models.Sponsors.Input;

public class CreateSendAlsoToInputModelMappingDefinitionsTests : MappingTestBase
{
    [Fact]
    public void Maps_All_Fields_And_Normalizes_Email()
    {
        var input = new CreateSendAlsoToInputModel("  luIs pérez ", "  AnA@x.COM ", false);

        var result = input.Adapt<CreateSendAlsoToInputDataModel>(Config);

        result.RecipientName.Should().Be("Luis Pérez");
        result.RecipientEmail.Should().Be("ana@x.com");
        result.SendAsBcc.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RecipientName_MustBeNonEmpty(string name)
    {
        var input = new CreateSendAlsoToInputModel(name, "ana@x.com", false);

        var result = () => input.Adapt<CreateSendAlsoToInputDataModel>(Config);

        result.Should().Throw<ArgumentException>()
            .WithMessage("String must be non-empty. (Parameter 'value')");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RecipientEmail_MustBeNonEmpty(string email)
    {
        var input = new CreateSendAlsoToInputModel("Ana Perez", email, false);

        var result = () => input.Adapt<CreateSendAlsoToInputDataModel>(Config);

        result.Should().Throw<ArgumentException>()
            .WithMessage("String must be non-empty. (Parameter 'value')");
    }
}