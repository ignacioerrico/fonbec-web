using FluentAssertions;
using Fonbec.Web.DataAccess.DataModels.Sponsors.Input;
using Fonbec.Web.Logic.Models.Sponsors.Input;
using Mapster;

namespace Fonbec.Web.Logic.Tests.Models.Sponsors.Input;

public class UpdateSponsorSendAlsoTosInputModelMappingDefinitionsTests : MappingTestBase
{
    [Fact]
    public void Maps_Recipients_And_Normalizes_Fields()
    {
        var input = new UpdateSponsorSendAlsoTosInputModel(
            SponsorId: 7,
            ChapterId: 3,
            Recipients:
            [
                new UpdateSendAlsoToInputModel(4, "  luIs pérez ", "  LuIs@Ejemplo.COM ", true),
                new UpdateSendAlsoToInputModel(0, "Ana Gomez", "ana@x.com", false),
            ],
            UpdatedById: 12);

        var result = input.Adapt<UpdateSponsorSendAlsoTosInputDataModel>(Config);

        result.SponsorId.Should().Be(7);
        result.ChapterId.Should().Be(3);
        result.UpdatedById.Should().Be(12);
        result.Recipients.Should().HaveCount(2);
        result.Recipients[0].Id.Should().Be(4);
        result.Recipients[0].RecipientName.Should().Be("Luis Pérez");
        result.Recipients[0].RecipientEmail.Should().Be("luis@ejemplo.com");
        result.Recipients[0].SendAsBcc.Should().BeTrue();
        result.Recipients[1].Id.Should().Be(0);
        result.Recipients[1].SendAsBcc.Should().BeFalse();
    }

    [Fact]
    public void Maps_Null_Chapter_For_Admin()
    {
        var input = new UpdateSponsorSendAlsoTosInputModel(7, null, [], 12);

        var result = input.Adapt<UpdateSponsorSendAlsoTosInputDataModel>(Config);

        result.ChapterId.Should().BeNull();
        result.Recipients.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RecipientEmail_MustBeNonEmpty(string email)
    {
        var input = new UpdateSendAlsoToInputModel(1, "Ana Perez", email, false);

        var result = () => input.Adapt<UpdateSendAlsoToInputDataModel>(Config);

        result.Should().Throw<ArgumentException>()
            .WithMessage("String must be non-empty. (Parameter 'value')");
    }
}